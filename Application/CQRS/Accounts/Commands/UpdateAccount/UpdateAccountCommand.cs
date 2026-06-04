using Domain.Enums;
using MediatR;

namespace Application.CQRS.Accounts.Commands.UpdateAccount
{
    public record UpdateAccountCommand(
        int Id,
        string Login,
        string Password,
        string? Description,
        string? FirstName,
        string? LastName,
        LoginMethod LoginMethod,
        string PhoneNumber,
        int? ProxyId = null) : IRequest;
}
