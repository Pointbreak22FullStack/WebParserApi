using MediatR;

namespace Application.CQRS.Accounts.Commands.DeleteAccount
{
    public record DeleteAccountCommand(int Id) : IRequest;
}
