using Application.Common.Notifications;
using Application.CQRS.Proxy.DTOs;
using Domain.Interfaces;
using MediatR;
using ProxyEntity = Domain.Entities.Proxy;

namespace Application.CQRS.Proxy.Commands.CreateProxy
{
    public class CreateProxyHandler : IRequestHandler<CreateProxyCommand, int>
    {
        private readonly IProxyRepository _repository;
        private readonly IPublisher _publisher;

        public CreateProxyHandler(IProxyRepository repository, IPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task<int> Handle(CreateProxyCommand request, CancellationToken ct)
        {
            var proxy = ProxyEntity.Create(
                request.IpAddress, request.Port, request.Login, request.Password, request.Type);

            await _repository.AddAsync(proxy);

            var dto = new ProxyDto(proxy.Id, proxy.IpAddress, proxy.Port, proxy.Login,
                                   proxy.Password, proxy.Type, proxy.IsActive, proxy.LatencyMs, 0,
                                   proxy.HasError, proxy.IsWarming, proxy.IsFree);

            await _publisher.Publish(
                new EntityChangedNotification("proxies", ChangeType.Created, dto), ct);

            return proxy.Id;
        }
    }
}
