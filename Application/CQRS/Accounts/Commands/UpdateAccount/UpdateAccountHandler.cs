using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Accounts.Commands.CreateAccount;
using Domain.Entities;
using MediatR;

namespace Application.CQRS.Accounts.Commands.UpdateAccount
{
    public class UpdateAccountHandler : IRequestHandler<UpdateAccountCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IPublisher _publisher;

        public UpdateAccountHandler(IAccountRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task Handle(UpdateAccountCommand request, CancellationToken ct)
        {
            var account = await _repository.GetByIdAsync(request.Id)
                ?? throw new NotFoundException(nameof(Account), request.Id);

            account.UpdateFromForm(
                request.Login, request.Password, request.Description,
                request.FirstName, request.LastName,
                request.LoginMethod, request.PhoneNumber,
                request.ProxyId);

            await _repository.UpdateAsync(account);

            var updated = await _repository.GetByIdWithProxyAsync(request.Id);
            var dto = CreateAccountHandler.MapToDto(updated!);
            await _publisher.Publish(
                new EntityChangedNotification("accounts", ChangeType.Updated, dto), ct);
        }
    }
}
