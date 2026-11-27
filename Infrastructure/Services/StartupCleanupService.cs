using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// При старте приложения сбрасывает аккаунты и прокси, зависшие в InWork из-за предыдущего краша:
    /// удаляет Redis-локи и возвращает аккаунтам статус Active.
    /// </summary>
    public class StartupCleanupService : IHostedService
    {
        private readonly IServiceScopeFactory          _scopeFactory;
        private readonly IAccountLockService           _accountLockService;
        private readonly IProxyLockService             _proxyLockService;
        private readonly ILogger<StartupCleanupService> _logger;

        public StartupCleanupService(
            IServiceScopeFactory scopeFactory,
            IAccountLockService accountLockService,
            IProxyLockService proxyLockService,
            ILogger<StartupCleanupService> logger)
        {
            _scopeFactory       = scopeFactory;
            _accountLockService = accountLockService;
            _proxyLockService   = proxyLockService;
            _logger             = logger;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.Database.MigrateAsync(ct);

            await EncryptLegacyPasswordsAsync(db, ct);

            // Сбрасываем зависшие аккаунты
            var stuckAccounts = await db.Accounts
                .Where(a => a.Status == AccountStatus.InWork)
                .ToListAsync(ct);

            if (stuckAccounts.Count > 0)
            {
                _logger.LogWarning("[Startup] Найдено {N} аккаунтов в статусе InWork — сбрасываю...", stuckAccounts.Count);

                foreach (var account in stuckAccounts)
                {
                    await _accountLockService.ForceReleaseAsync(account.Id, ct);
                    account.Status               = AccountStatus.Active;
                    account.CurrentHandlerUserId = null;
                }

                await db.SaveChangesAsync(ct);
                _logger.LogInformation("[Startup] Сброшено {N} зависших аккаунтов.", stuckAccounts.Count);
            }

            // Сбрасываем Redis-локи для всех прокси (на случай краша во время задачи)
            var allProxyIds = await db.Proxies.Select(p => p.Id).ToListAsync(ct);
            foreach (var proxyId in allProxyIds)
                await _proxyLockService.ForceReleaseAsync(proxyId, ct);

            if (allProxyIds.Count > 0)
                _logger.LogInformation("[Startup] Сброшены Redis-локи для {N} прокси.", allProxyIds.Count);
        }

        /// <summary>
        /// Разовая миграция: пароли, сохранённые до включения шифрования, перезаписываются зашифрованными.
        /// </summary>
        private async Task EncryptLegacyPasswordsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            const string legacyFilter = "\"Password\" <> '' AND \"Password\" NOT LIKE 'enc:v1:%'";

            var accountIds = await db.Database
                .SqlQueryRaw<int>($"SELECT \"Id\" AS \"Value\" FROM \"Accounts\" WHERE {legacyFilter}")
                .ToListAsync(ct);
            var proxyIds = await db.Database
                .SqlQueryRaw<int>($"SELECT \"Id\" AS \"Value\" FROM \"Proxies\" WHERE {legacyFilter}")
                .ToListAsync(ct);

            if (accountIds.Count == 0 && proxyIds.Count == 0)
                return;

            var accounts = await db.Accounts.Where(a => accountIds.Contains(a.Id)).ToListAsync(ct);
            foreach (var account in accounts)
                db.Entry(account).Property(a => a.Password).IsModified = true;

            var proxies = await db.Proxies.Where(p => proxyIds.Contains(p.Id)).ToListAsync(ct);
            foreach (var proxy in proxies)
                db.Entry(proxy).Property(p => p.Password).IsModified = true;

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[Startup] Зашифрованы пароли: аккаунтов {A}, прокси {P}.", accounts.Count, proxies.Count);
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
