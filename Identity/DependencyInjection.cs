using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityServer(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("IdentityDb");

            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(typeof(AppIdentityDbContext).Assembly.GetName().Name));
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Отключаем подтверждение почты для тестов
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Настройки пароля (сделайте проще для отладки)
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 3;
            })
                  .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "AUTH_SESSION"; // Понятное имя
                options.Cookie.HttpOnly = true;       // JS не видит куку (безопасно)
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.Path = "/";


                options.LoginPath = "/identity/account/login";

                options.Events.OnRedirectToLogin = context =>
                {
                    bool isApiOrConnect =           
                      context.Request.Path.StartsWithSegments("/api") ||  
                      context.Request.Path.StartsWithSegments("/connect") ||    
                      context.Request.Path.StartsWithSegments("/identity/connect");

                    // Если запрос идет к эндпоинтам OpenIddict, не нужно принудительно ставить 401,
                    // иначе механизм Challenge в контроллере не сработает.
                    // Если это API запрос (начинается с /api или /connect)
                    if (isApiOrConnect && !context.Request.Path.StartsWithSegments("/identity/connect/authorize"))
                    {
                        // API-запросы без куки → 401 (не редирект)
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    else
                    {
                        // Authorize endpoint и обычные страницы → редирект на логин
                        context.Response.Redirect(context.RedirectUri);
                    }
                    return Task.CompletedTask;
                };
            });

            services.AddOpenIddict()  
                .AddCore(options =>             
                {                               
                    options.UseEntityFrameworkCore()  
                    .UseDbContext<AppIdentityDbContext>(); 
                })                                  
                .AddServer(options =>            
                {                     
                    
                    options.SetAuthorizationEndpointUris("/identity/connect/authorize");  
                    options.SetTokenEndpointUris("/identity/connect/token");   
                    
                    options.AllowAuthorizationCodeFlow();      // 🔥 вместо password
                    options.RequireProofKeyForCodeExchange();  // PKCE обязателен

                    options.AllowRefreshTokenFlow(); // если хочешь refresh tokens
                    options.AllowPasswordFlow(); // 🔥 ОБЯЗАТЕЛЬНО добавить
                    options.AcceptAnonymousClients(); // ← ОБЯЗАТЕЛЬНО для SPA
                    options.DisableTokenStorage();
                    options.RegisterScopes(     
                        Scopes.OpenId,          
                        Scopes.Profile,          
                        Scopes.Email,             
                        Scopes.Roles,        
                        "resource_api"); 
                    options.AddDevelopmentEncryptionCertificate()   
                    .AddDevelopmentSigningCertificate();

                    options.DisableAccessTokenEncryption(); // 🔥 ВОТ ЭТО

                    options.UseAspNetCore()             
                    .EnableTokenEndpointPassthrough()      
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableStatusCodePagesIntegration(); // Помогает корректно пробрасывать 401/404
                })                                 
                .AddValidation(options =>        
                {                                   
                    options.UseLocalServer();         
                    options.UseAspNetCore();        
                });

            return services;
        }
    }
}
