using MediatR;

namespace Application.CQRS.Proxy.Commands.CreateProxy
{
    public record CreateProxyCommand(
        string IpAddress,
        int Port,
        string Login,
        string Password,
        Domain.Enums.ProxyType Type) : IRequest<int>;
}
