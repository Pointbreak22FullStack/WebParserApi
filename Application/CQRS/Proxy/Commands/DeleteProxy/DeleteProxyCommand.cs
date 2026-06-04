using MediatR;

namespace Application.CQRS.Proxy.Commands.DeleteProxy
{
    public record DeleteProxyCommand(int Id) : IRequest;
}
