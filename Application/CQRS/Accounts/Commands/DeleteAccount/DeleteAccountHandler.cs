using Application.Common.Interfaces;
using Application.Common.Notifications;
using MediatR;

namespace Application.CQRS.Accounts.Commands.DeleteAccount
{
    public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IPublisher _publisher;

        public DeleteAccountHandler(IAccountRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
        {
            await _repository.DeleteAsync(request.Id);
            await _publisher.Publish(
                new EntityChangedNotification("accounts", ChangeType.Deleted, request.Id), ct);
        }
    }
}
