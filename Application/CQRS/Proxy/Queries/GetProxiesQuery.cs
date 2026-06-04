using Application.CQRS.Proxy.DTOs;
using MediatR;

namespace Application.CQRS.Proxy.Queries
{
    public record GetProxiesQuery : IRequest<IEnumerable<ProxyDto>>;
}
