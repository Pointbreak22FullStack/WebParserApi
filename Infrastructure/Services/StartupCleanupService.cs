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

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
