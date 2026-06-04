using Application.CQRS.Proxy.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Proxy.Queries
{
    public class GetProxiesHandler : IRequestHandler<GetProxiesQuery, IEnumerable<ProxyDto>>
    {
        private readonly IProxyRepository _repository;
        public GetProxiesHandler(IProxyRepository repository) => _repository = repository;

        public async Task<IEnumerable<ProxyDto>> Handle(GetProxiesQuery request, CancellationToken ct)
        {
            var proxies = await _repository.GetAllAsync();

            return proxies.Select(p => new ProxyDto(
                p.Id,
                p.IpAddress,
                p.Port,
                p.Login,
                p.Password,
                p.Type,
                p.IsActive,
                p.LatencyMs,
                p.Accounts.Count,
                p.HasError,
                p.IsWarming,
                p.IsFree));
        }
    }
}
