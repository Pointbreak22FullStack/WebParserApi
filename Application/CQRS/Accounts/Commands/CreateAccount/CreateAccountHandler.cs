using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Accounts.DTOs;
using Domain.Entities;
using MediatR;

namespace Application.CQRS.Accounts.Commands.CreateAccount
{
    public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, int>
    {
        private readonly IAccountRepository _repository;
        private readonly IPublisher _publisher;

        public CreateAccountHandler(IAccountRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task<int> Handle(CreateAccountCommand request, CancellationToken ct)
        {
            var account = new Account(
                request.Login, request.Password, request.Description,
                request.FirstName, request.LastName,
                request.LoginMethod, request.PhoneNumber,
                request.ProxyId);

            await _repository.AddAsync(account);

            var dto = MapToDto(account);
            await _publisher.Publish(
                new EntityChangedNotification("accounts", ChangeType.Created, dto), ct);

            return account.Id;
        }

        public static AccountDto MapToDto(Account a) => new(
            // Пароль наружу не отдаём: клиент получает пустую строку
            a.Id, a.Login, string.Empty, a.Description, a.FirstName, a.LastName,
            a.Status, a.CompletedTasksCount, a.LastUsedAtUtc,
            a.ProxyId, a.Proxy?.IpAddress, a.CreatedAt,
            a.LoginMethod, a.PhoneNumber, a.SessionInfo, a.SessionExpiresAtUtc, a.RiskScore);
    }
}
