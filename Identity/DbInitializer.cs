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

        // 1️⃣ Роли
        string[] roles = { "Admin", "user" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2️⃣ Админ
        var adminEmail = "admin@admin.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true ,
                 FullName = "Administrator"
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // 3️⃣ OpenIddict Client
        if (await appManager.FindByClientIdAsync("angular_spa") == null)
        {
            var existingApp = await appManager.FindByClientIdAsync("angular_spa");
            if (existingApp != null)
            {
                await appManager.DeleteAsync(existingApp);
            }

            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "angular_spa",
                DisplayName = "Angular SPA",

                ClientType = ClientTypes.Public,

                RedirectUris =           
                {                                                     
                    new Uri("http://localhost:4200/auth/callback"),
                    new Uri("https://c469-217-19-214-254.ngrok-free.app/auth/callback") // ✅ добавить

                },
                Permissions =             
                {
                 // endpoints
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,

                    // grant types
                    Permissions.GrantTypes.AuthorizationCode,

                    // PKCE
                   
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,

                    Permissions.ResponseTypes.Code,  
                    // scopes
                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Prefixes.Scope + Scopes.Profile,
                    Permissions.Prefixes.Scope + Scopes.Email,
                    Permissions.Prefixes.Scope + Scopes.Roles,
                    Permissions.Prefixes.Scope + "resource_api"
                }
            });
        }
    }
}

