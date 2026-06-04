using Domain.Enums;
using MediatR;

namespace Application.CQRS.Accounts.Commands.CreateAccount
{
    public record CreateAccountCommand(
        string Login,
        string Password,
        string? Description,
        string? FirstName,
        string? LastName,
        LoginMethod LoginMethod,
        string PhoneNumber,
        int? ProxyId = null) : IRequest<int>;
}
