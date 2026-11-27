using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Notifications;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services.Parser.Authorize;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Security.Cryptography;

namespace Infrastructure.Services.Parser
{
    /// <summary>
    /// Singleton-оркестратор параллельного запуска аккаунтов.
    ///
    /// Гарантии:
    ///   – Не более MaxParallelTasks одновременных активных аккаунтов (глобально).
    ///   – Аккаунты и прокси не пересекаются: Redis-лок захватывается и на аккаунт, и на его прокси.
    ///   – Прокси проверяется TCP-соединением перед использованием; при ошибке — аккаунт пропускается.
    ///   – Если рабочих прокси/аккаунтов меньше Count — задача выполняется с тем, что доступно.
    ///   – TTL лока — 2 часа.
    /// </summary>
    public class AccountActivator
    {
        private volatile SemaphoreSlim? _semaphore;
        private readonly SemaphoreSlim  _semaphoreInit = new(1, 1);

        private readonly IServiceScopeFactory      _scopeFactory;
        private readonly BrowserManager            _browserManager;
        private readonly ICodeStore                _codeStore;
        private readonly IAccountLockService       _lockService;
        private readonly IProxyLockService         _proxyLockService;
        private readonly ProxyTester               _proxyTester;
        private readonly IPublisher                _publisher;
        private readonly ILogger<AccountActivator> _logger;

        private static readonly TimeSpan LockTtl = TimeSpan.FromHours(2);

        public AccountActivator(
            IServiceScopeFactory scopeFactory,
            BrowserManager browserManager,
            ICodeStore codeStore,
            IAccountLockService lockService,
            IProxyLockService proxyLockService,
            ProxyTester proxyTester,
            IPublisher publisher,
            ILogger<AccountActivator> logger)
        {
            _scopeFactory     = scopeFactory;
            _browserManager   = browserManager;
            _codeStore        = codeStore;
            _lockService      = lockService;
            _proxyLockService = proxyLockService;
            _proxyTester      = proxyTester;
            _publisher        = publisher;
            _logger           = logger;
        }

        /// <summary>
        /// Запускает до task.Count аккаунтов параллельно (не более MaxParallelTasks одновременно).
        /// </summary>
        public async Task RunAsync(
            UserTask task,
            Func<Account, IBrowserContext, ITaskLogger, CancellationToken, Task> action,
            CancellationToken ct)
        {
            var semaphore = await GetOrCreateSemaphoreAsync(ct);
            var startedAt = DateTime.UtcNow;

            var claimed = await SelectAndClaimAccountsAsync(task, startedAt, ct);
            if (claimed.Count == 0) return;

            var totalAccounts = claimed.Count;
            var completedRef  = new int[] { 0 };

            var jobs = claimed.Select(((int AccountId, int? ProxyId) entry, int index) => Task.Run(async () =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    await ProcessAccountAsync(
                        task, entry.AccountId, entry.ProxyId,
                        index, totalAccounts, startedAt, completedRef, action, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));

            await Task.WhenAll(jobs);
        }

        private async Task ProcessAccountAsync(
            UserTask task,
            int accountId,
            int? proxyId,
            int accountIndex,
            int totalAccounts,
            DateTime startedAt,
            int[] completedRef,
            Func<Account, IBrowserContext, ITaskLogger, CancellationToken, Task> action,
            CancellationToken ct)
        {
            using var scope  = _scopeFactory.CreateScope();
            var accountRepo  = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var loginFactory = scope.ServiceProvider.GetRequiredService<LoginFlowFactory>();
            var dbContext    = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
            var publisher    = scope.ServiceProvider.GetRequiredService<IPublisher>();

            var account = await accountRepo.GetByIdWithProxyAsync(accountId);
            if (account == null)
            {
                _logger.LogError("[AccountActivator] Аккаунт {Id} не найден после захвата.", accountId);
                await _lockService.ReleaseAsync(accountId, ct);
                if (proxyId.HasValue)
                    await _proxyLockService.ReleaseAsync(proxyId.Value, ct);
                return;
            }

            var taskLog = new TaskLogger(publisher, task.Id, task.UserId, account.Login, startedAt, totalAccounts, accountIndex, completedRef);

            var taskAccount = new Domain.Entities.TaskAccount(task.Id, accountId);
            taskAccount.Status = Domain.Enums.TaskAccountStatus.InWork;
            await dbContext.TaskAccounts.AddAsync(taskAccount, ct);
            await dbContext.SaveChangesAsync(ct);

            var started = DateTime.UtcNow;

            await taskLog.InfoAsync($"Аккаунт '{account.Login}': начало обработки.");
            taskLog.SetProgress(0);

            if (account.Proxy != null)
            {
                await taskLog.InfoAsync(
                    $"Вход с использованием прокси {account.Proxy.IpAddress}:{account.Proxy.Port} ({account.Proxy.Type}).");
            }

            var context = await _browserManager.CreateContextAsync(account.Proxy, account.SessionInfo, ct);
            try
            {
                var loginFlow = loginFactory.GetFor(account.LoginMethod);

                await taskLog.InfoAsync("Открываю браузер...");

                if (!string.IsNullOrEmpty(account.PhoneNumber))
                    _codeStore.RegisterPhone(accountId, account.PhoneNumber);

                await loginFlow.LoginAsync(context, account, task.TargetUrl,
                    () => _codeStore.WaitForCodeAsync(accountId, account.PhoneNumber, ct),
                    new ActionDelay(task.Delay),
                    taskLog,
                    ct);

                await accountRepo.UpdateAsync(account);
                _logger.LogInformation("[AccountActivator] Сессия аккаунта {Login} сохранена.", account.Login);

                await new ActionDelay(task.Delay).WaitAsync(ct);

                taskLog.SetProgress(85);
                await taskLog.InfoAsync("Выполняю действие задачи...");

                await action(account, context, taskLog, ct);

                taskLog.MarkCompleted();
                taskLog.SetProgress(100);
                await taskLog.SuccessAsync($"Аккаунт '{account.Login}': задача выполнена успешно.");

                account.IncrementTaskCount();
                account.Status               = AccountStatus.Active;
                account.CurrentHandlerUserId = null;

                taskAccount.Status          = Domain.Enums.TaskAccountStatus.Completed;
                taskAccount.CompletedAt     = DateTime.UtcNow;
                taskAccount.DurationSeconds = (int)(DateTime.UtcNow - started).TotalSeconds;

                _logger.LogInformation("[AccountActivator] Аккаунт {Login} завершил задачу {TaskId}.", account.Login, task.Id);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("[AccountActivator] Задача {TaskId} отменена, аккаунт {Login} остановлен.", task.Id, account.Login);
                await taskLog.InfoAsync($"Аккаунт '{account.Login}': задача отменена.");
                taskLog.MarkCompleted();

                account.Status               = AccountStatus.Active;
                account.CurrentHandlerUserId = null;

                taskAccount.Status          = Domain.Enums.TaskAccountStatus.Error;
                taskAccount.CompletedAt     = DateTime.UtcNow;
                taskAccount.DurationSeconds = (int)(DateTime.UtcNow - started).TotalSeconds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AccountActivator] Ошибка при работе с аккаунтом {Login}.", account.Login);
                var errorMsg = ex.InnerException is not null
                    ? $"{ex.Message} → {ex.InnerException.Message}"
                    : ex.Message;
                await taskLog.ErrorAsync($"Аккаунт '{account.Login}': ошибка — {errorMsg}");
                taskLog.MarkCompleted();

                account.Status               = AccountStatus.Active;
                account.CurrentHandlerUserId = null;

                taskAccount.Status          = Domain.Enums.TaskAccountStatus.Error;
                taskAccount.CompletedAt     = DateTime.UtcNow;
                taskAccount.DurationSeconds = (int)(DateTime.UtcNow - started).TotalSeconds;
            }
            finally
            {
                // CancellationToken.None — cleanup должен выполниться всегда,
                // даже если задача была отменена или сработал таймаут Playwright
                _codeStore.Release(accountId);
                await context.CloseAsync();
                await _lockService.ReleaseAsync(accountId, CancellationToken.None);
                if (account.ProxyId.HasValue)
                    await _proxyLockService.ReleaseAsync(account.ProxyId.Value, CancellationToken.None);

                // Use a fresh scope: the original dbContext may be invalidated if a transient
                // DB error occurred earlier, which would cause all further operations on the same
                // context instance to throw InvalidOperationException.
                using var cleanupScope = _scopeFactory.CreateScope();
                var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                cleanupDb.Accounts.Update(account);
                cleanupDb.Entry(taskAccount).State = EntityState.Modified;
                await cleanupDb.SaveChangesAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Выбирает и захватывает до task.Count аккаунтов с тестированием прокси и Redis-локами.
        /// Возвращает список (accountId, proxyId).
        /// </summary>
        private async Task<List<(int AccountId, int? ProxyId)>> SelectAndClaimAccountsAsync(
            UserTask task, DateTime startedAt, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

            var allActive = (await accountRepo.GetActiveAccountsWithProxyAsync()).ToList();

            if (allActive.Count == 0)
            {
                _logger.LogWarning("[AccountActivator] Нет аккаунтов со статусом Active для задачи {TaskId}.", task.Id);
                await PublishSystemLogAsync(task, startedAt, "error",
                    "Нет доступных аккаунтов (статус Active). Проверьте аккаунты в настройках.");
                return [];
            }

            // Аккаунты с активным прокси (HasError не проверяем здесь — тестируем сами ниже)
            var withProxy = allActive
                .Where(a => a.Proxy is { IsActive: true })
                .ToList();

            // Аккаунты без прокси — фолбек
            var noProxy = allActive
                .Where(a => a.ProxyId == null)
                .OrderBy(a => a.CompletedTasksCount)
                .ToList();

            // Тестируем уникальные прокси параллельно
            var uniqueProxies = withProxy
                .Select(a => a.Proxy!)
                .DistinctBy(p => p.Id)
                .ToList();

            var testResults = await Task.WhenAll(uniqueProxies.Select(async p =>
            {
                await PublishSystemLogAsync(task, startedAt, "info",
                    $"Проверка прокси {p.IpAddress}:{p.Port}...");

                var ok = await _proxyTester.TestAsync(p, ct);

                if (ok)
                    await PublishSystemLogAsync(task, startedAt, "info",
                        $"Прокси {p.IpAddress}:{p.Port} — прошёл проверку.");
                else
                {
                    _logger.LogWarning("[AccountActivator] Прокси {Ip}:{Port} не прошёл проверку.", p.IpAddress, p.Port);
                    await PublishSystemLogAsync(task, startedAt, "error",
                        $"Прокси {p.IpAddress}:{p.Port} — не прошёл проверку, аккаунты пропускаются.");
                }

                return (Proxy: p, Ok: ok);
            }));

            var workingProxyIds = testResults
                .Where(r => r.Ok)
                .Select(r => r.Proxy.Id)
                .ToHashSet();

            // Группировка аккаунтов с рабочими прокси
            var byProxy = withProxy
                .Where(a => workingProxyIds.Contains(a.ProxyId!.Value))
                .GroupBy(a => a.ProxyId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(a => a.CompletedTasksCount)
                          .ThenBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                          .ToList());

            // Round-robin выбор аккаунтов по прокси-группам
            var candidates = new List<Account>(task.Count);
            int maxRounds  = byProxy.Values.Count > 0 ? byProxy.Values.Max(l => l.Count) : 0;

            for (int round = 0; round < maxRounds && candidates.Count < task.Count; round++)
            {
                foreach (var accounts in byProxy.Values)
                {
                    if (round < accounts.Count)
                        candidates.Add(accounts[round]);
                    if (candidates.Count >= task.Count) break;
                }
            }

            // Фолбек: аккаунты без прокси, если слотов ещё нет
            var candidateIds = candidates.Select(a => a.Id).ToHashSet();
            foreach (var acc in noProxy.Where(a => !candidateIds.Contains(a.Id)))
            {
                if (candidates.Count >= task.Count) break;
                candidates.Add(acc);
            }

            if (candidates.Count == 0)
            {
                _logger.LogWarning("[AccountActivator] Нет подходящих аккаунтов для задачи {TaskId}.", task.Id);
                await PublishSystemLogAsync(task, startedAt, "error",
                    "Нет подходящих аккаунтов: все прокси недоступны и нет аккаунтов без прокси.");
                return [];
            }

            if (candidates.Count < task.Count)
            {
                _logger.LogWarning(
                    "[AccountActivator] Доступно только {Avail}/{Required} аккаунтов для задачи {TaskId}.",
                    candidates.Count, task.Count, task.Id);
                await PublishSystemLogAsync(task, startedAt, "info",
                    $"Доступно {candidates.Count} из {task.Count} запрошенных аккаунтов — выполняем с тем, что есть.");
            }

            // Захватываем Redis-локи: сначала прокси, потом аккаунт
            var claimed = new List<(int AccountId, int? ProxyId)>(candidates.Count);

            foreach (var account in candidates)
            {
                if (claimed.Count >= task.Count) break;

                if (account.ProxyId.HasValue)
                {
                    if (!await _proxyLockService.TryAcquireAsync(account.ProxyId.Value, LockTtl, ct))
                    {
                        _logger.LogDebug(
                            "[AccountActivator] Прокси {ProxyId} занят другой задачей, пропускаю аккаунт {Login}.",
                            account.ProxyId.Value, account.Login);
                        continue;
                    }
                }

                if (!await _lockService.TryAcquireAsync(account.Id, LockTtl, ct))
                {
                    if (account.ProxyId.HasValue)
                        await _proxyLockService.ReleaseAsync(account.ProxyId.Value, ct);
                    continue;
                }

                account.Status               = AccountStatus.InWork;
                account.CurrentHandlerUserId = task.UserId;
                await accountRepo.UpdateAsync(account);
                claimed.Add((account.Id, account.ProxyId));
            }

            if (claimed.Count == 0)
            {
                _logger.LogWarning("[AccountActivator] Все аккаунты заняты (Redis-лок) для задачи {TaskId}.", task.Id);
                await PublishSystemLogAsync(task, startedAt, "error",
                    "Все подходящие аккаунты сейчас заняты другими задачами. Попробуйте позже.");
            }
            else
            {
                _logger.LogInformation(
                    "[AccountActivator] Захвачено {N}/{Count} аккаунтов для задачи {TaskId}.",
                    claimed.Count, task.Count, task.Id);
            }

            return claimed;
        }

        /// <summary>Публикует системное сообщение в лог задачи (без конкретного аккаунта).</summary>
        private Task PublishSystemLogAsync(UserTask task, DateTime startedAt, string status, string message)
        {
            var entry = new TaskLogEntry(
                task.Id,
                task.UserId,
                "система",
                0.0,
                (DateTime.UtcNow - startedAt).ToString(@"hh\:mm\:ss"),
                status,
                message,
                DateTime.UtcNow);

            return _publisher.Publish(new TaskLogNotification($"user-logs:{task.UserId}", entry));
        }

        private async Task<SemaphoreSlim> GetOrCreateSemaphoreAsync(CancellationToken ct)
        {
            if (_semaphore is { } s) return s;

            await _semaphoreInit.WaitAsync(ct);
            try
            {
                if (_semaphore is { } s2) return s2;

                using var scope  = _scopeFactory.CreateScope();
                var settingsRepo = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();
                var settings     = await settingsRepo.GetAsync(ct);
                var maxParallel  = settings?.MaxParallelTasks ?? 10;

                _logger.LogInformation("Семафор AccountActivator: maxParallel={Max}.", maxParallel);
                return _semaphore = new SemaphoreSlim(maxParallel, maxParallel);
            }
            finally
            {
                _semaphoreInit.Release();
            }
        }
    }
}
