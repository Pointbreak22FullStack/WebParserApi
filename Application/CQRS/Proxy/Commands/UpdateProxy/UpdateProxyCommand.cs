using MediatR;

namespace Application.CQRS.Proxy.Commands.UpdateProxy
{
    public record UpdateProxyCommand(
        int Id,
        string IpAddress,
        int Port,
        string Login,
        string Password,
        Domain.Enums.ProxyType Type,
        bool IsActive) : IRequest;
}
