using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Controllers;

[ApiController]
[Route("identity")]
public class AuthorizationController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
     SignInManager<ApplicationUser> signInManager,
     UserManager<ApplicationUser> userManager,
     ILogger<AuthorizationController> logger) // Добавьте ILogger
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }



    [HttpGet("connect/authorize")]
    [HttpPost("connect/authorize")]

    [IgnoreAntiforgeryToken] // Важно для кросс-доменных POST запросов
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Проверяем куку Identity
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            return Challenge(IdentityConstants.ApplicationScheme);
        }

        var user = await _userManager.GetUserAsync(result.Principal) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id);
        identity.AddClaim(Claims.Name, user.UserName!);

        // 🔴 ДОБАВЛЯЕМ ЭТУ СТРОКУ 🔴
        if (!string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(Claims.Email, user.Email);
        }

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources("resource_api");

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal.GetScopes().ToArray()));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    // 🔴 ДОБАВЛЯЕМ ЭТОТ МЕТОД 🔴
    [HttpPost("connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // 1. Для Password Flow (через форму Angular)
        if (request.IsPasswordGrantType())
        {
            var user = await _userManager.FindByNameAsync(request.Username);

            if (user == null)
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
         user, request.Password, lockoutOnFailure: false);

            if (user == null || !signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed for user {Username}. Result: {Result}", request.Username, signInResult);
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role);

            identity.AddClaim(Claims.Subject, user.Id);
            identity.AddClaim(Claims.Name, user.UserName!);
            identity.AddClaim(Claims.Email, user.Email!);

            // ✅ ВАЖНО: добавить роли (раньше их не было в password flow!)
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
                identity.AddClaim(Claims.Role, role);


            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            principal.SetResources("resource_api");

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal.GetScopes().ToArray()));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // 2. Для Code Flow (если вдруг используешь)
        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // ... твой существующий код ...
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            // ...
            return SignIn(result.Principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = Errors.UnsupportedGrantType });
    }

    private static IEnumerable<string> GetDestinations(Claim claim, string[] scopes)
    {
        switch (claim.Type)
        {
            case Claims.Name:
            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (scopes.Contains(Scopes.Roles))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (scopes.Contains(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
