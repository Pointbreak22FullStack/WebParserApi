using Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключение к базе данных
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

// Настройка Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

// Настройка IdentityServer
builder.Services.AddIdentityServer()
    .AddAspNetIdentity<ApplicationUser>()
    .AddInMemoryClients(new[] {
        new Duende.IdentityServer.Models.Client {
            ClientId = "web_client",
            AllowedGrantTypes = Duende.IdentityServer.Models.GrantTypes.ResourceOwnerPassword,
            ClientSecrets = { new Duende.IdentityServer.Models.Secret("secret".Sha256()) },
            AllowedScopes = { "openid", "profile", "api1" }
        }
    })
    .AddInMemoryApiScopes(new[] {
        new Duende.IdentityServer.Models.ApiScope("api1", "My API")
    })
    .AddInMemoryIdentityResources(new Duende.IdentityServer.Models.IdentityResource[] {
        new Duende.IdentityServer.Models.IdentityResources.OpenId(),
        new Duende.IdentityServer.Models.IdentityResources.Profile()
    })
    .AddDeveloperSigningCredential();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
