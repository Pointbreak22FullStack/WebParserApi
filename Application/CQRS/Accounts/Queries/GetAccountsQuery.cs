using Application.CQRS.Accounts.DTOs;
using MediatR;

namespace Application.CQRS.Accounts.Queries
{
    public record GetAccountsQuery : IRequest<IEnumerable<AccountDto>>;
}
