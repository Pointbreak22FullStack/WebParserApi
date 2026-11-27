using Application.Common.Interfaces;
using Domain.Interfaces;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Infrastructure.Data;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Infrastructure.Services.AI;
using Infrastructure.Services.Captcha;
using Infrastructure.Services;
using Infrastructure.Services.Parser;
using Infrastructure.Services.Parser.Processors;
using Infrastructure.Services.Parser.Authorize;
using Infrastructure.Services.Redis;
using Infrastructure.Services.Verification;
using Infrastructure.Services.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.Section));
            services.AddSingleton<SecretProtector>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name);
                    o.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                }));

            services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

            var redisConnection = configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";

            // Единственное соединение с Redis для всего приложения (thread-safe multiplexer)
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(redisConnection));
            services.AddSingleton<IAccountLockService, RedisAccountLockService>();
            services.AddSingleton<IProxyLockService, RedisProxyLockService>();
            services.AddSingleton<ProxyTester>();

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(redisConnection, new RedisStorageOptions
                {
                    Prefix = "autopanel_hangfire:",
                    Db = 0
                }));
            services.AddHangfireServer();

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IProxyRepository, ProxyRepository>();
            services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
            services.AddScoped<IBrowserService, PlaywrightBrowserService>();
            services.AddSingleton<BrowserManager>();
            services.AddSingleton<AccountActivator>();
            services.AddSingleton<ITaskCancellationRegistry, TaskCancellationRegistry>();
            services.AddHostedService<StartupCleanupService>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.Configure<CamoufoxOptions>(configuration.GetSection(CamoufoxOptions.Section));
            services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.Section));
            services.AddHttpClient<ClaudeAiService>(client =>
            {
                client.BaseAddress = new Uri("https://api.anthropic.com/");
                client.DefaultRequestHeaders.Add("x-api-key",
                    configuration[$"{ClaudeOptions.Section}:{nameof(ClaudeOptions.ApiKey)}"] ?? string.Empty);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            });
            services.AddScoped<IAiService, ClaudeAiService>();
            services.AddScoped<PlaywrightCaptchaSolver>();

            services.AddSingleton<ICodeStore, InMemoryCodeStore>();

            // Login flows (scoped). Входы Яндекса и Mail.ru удалены; новые площадки подключаются через официальный OAuth.
            services.AddScoped<LoginFlowFactory>();

            services.AddScoped<ITaskProcessor, CommentTaskProcessor>();
            services.AddScoped<ITaskExecutor, TaskExecutionService>();


            return services;
        }
    }
}
