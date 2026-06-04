using Application.Common.Notifications;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Proxy.Commands.DeleteProxy
{
    public class DeleteProxyHandler : IRequestHandler<DeleteProxyCommand>
    {
        private readonly IProxyRepository _repository;
        private readonly IPublisher _publisher;

        public DeleteProxyHandler(IProxyRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task Handle(DeleteProxyCommand request, CancellationToken ct)
        {
            await _repository.DeleteAsync(request.Id);

            await _publisher.Publish(
                new EntityChangedNotification("proxies", ChangeType.Deleted, request.Id), ct);
        }
    }
}
