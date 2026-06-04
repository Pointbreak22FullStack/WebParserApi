using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Роли
        foreach (var role in new[] { "Admin", "user" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Администратор
        var adminEmail = "admin@admin.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Administrator"
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // OpenIddict Client — пересоздаём при каждом старте чтобы redirect URIs всегда были актуальны
        var existingApp = await appManager.FindByClientIdAsync("angular_spa");
        if (existingApp != null)
            await appManager.DeleteAsync(existingApp);

        var redirectUris = (config["Identity:RedirectUris"] ?? "http://localhost:4200/auth/callback")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(u => new Uri(u))
            .ToList();

        var postLogoutUris = (config["Identity:PostLogoutRedirectUris"] ?? "http://localhost:4200/login")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(u => new Uri(u))
            .ToList();

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "angular_spa",
            DisplayName = "Angular SPA",
            ClientType = ClientTypes.Public,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Roles,
                Permissions.Prefixes.Scope + "resource_api"
            }
        };

        foreach (var uri in redirectUris)
            descriptor.RedirectUris.Add(uri);

        foreach (var uri in postLogoutUris)
            descriptor.PostLogoutRedirectUris.Add(uri);

        await appManager.CreateAsync(descriptor);
    }
}
