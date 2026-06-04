using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Accounts.Commands.CreateAccount;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Testing
{
    public class AccountStatusSimulatorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AccountStatusSimulatorService> _logger;

        private static readonly AccountStatus[] Cycle =
        [
            AccountStatus.Active,
            AccountStatus.Cooldown,
            AccountStatus.InWork,
            AccountStatus.CaptchaRequired,
            AccountStatus.Resting,
            AccountStatus.Banned,
        ];

        public AccountStatusSimulatorService(
            IServiceScopeFactory scopeFactory,
            ILogger<AccountStatusSimulatorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("AccountStatusSimulator started");

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);

                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
                    var publisher  = scope.ServiceProvider.GetRequiredService<IPublisher>();

                    var accounts = (await repository.GetAllAsync()).ToList();
                    if (accounts.Count == 0) continue;

                    var account = accounts[Random.Shared.Next(accounts.Count)];

                    var currentIndex = Array.IndexOf(Cycle, account.Status);
                    account.Status = Cycle[(currentIndex + 1) % Cycle.Length];

                    await repository.UpdateAsync(account);

                    var dto = CreateAccountHandler.MapToDto(account);
                    await publisher.Publish(
                        new EntityChangedNotification("accounts", ChangeType.Updated, dto), ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "AccountStatusSimulator error");
                }
            }
        }
    }
}
