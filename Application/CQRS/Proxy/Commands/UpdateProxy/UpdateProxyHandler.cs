using Application.Common.Exceptions;
using Application.Common.Notifications;
using Application.CQRS.Proxy.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Proxy.Commands.UpdateProxy
{
    public class UpdateProxyHandler : IRequestHandler<UpdateProxyCommand>
    {
        private readonly IProxyRepository _repository;
        private readonly IPublisher _publisher;

        public UpdateProxyHandler(IProxyRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task Handle(UpdateProxyCommand request, CancellationToken ct)
        {
            var proxy = await _repository.GetByIdAsync(request.Id)
                ?? throw new NotFoundException(nameof(Domain.Entities.Proxy), request.Id);

            // Пустой пароль в форме редактирования = оставить текущий
            var password = string.IsNullOrEmpty(request.Password) ? proxy.Password : request.Password;

            proxy.Update(request.IpAddress, request.Port, request.Login, password,
                         request.Type, request.IsActive);

            await _repository.UpdateAsync(proxy);

            var updated = await _repository.GetByIdWithAccountsAsync(request.Id);
            var dto = new ProxyDto(updated!.Id, updated.IpAddress, updated.Port, updated.Login,
                                   string.Empty, updated.Type, updated.IsActive, updated.LatencyMs,
                                   updated.Accounts.Count, updated.HasError, updated.IsWarming, updated.IsFree);

            await _publisher.Publish(
                new EntityChangedNotification("proxies", ChangeType.Updated, dto), ct);
        }
    }
}
