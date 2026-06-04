using MediatR;

namespace Application.CQRS.Verification.Commands.ReceiveCode
{
    public record ReceiveCodeCommand(string PhoneNumber, string Code) : IRequest;
}
