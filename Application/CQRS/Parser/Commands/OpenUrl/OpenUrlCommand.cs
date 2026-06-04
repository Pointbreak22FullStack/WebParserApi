using MediatR;

namespace Application.CQRS.Parser.Commands.OpenUrl
{
    public record OpenUrlCommand(string Url) : IRequest;
}
