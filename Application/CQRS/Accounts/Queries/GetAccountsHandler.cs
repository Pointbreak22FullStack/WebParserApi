using Application.Common.Interfaces;
using Application.CQRS.Accounts.DTOs;
using MediatR;

namespace Application.CQRS.Accounts.Queries
{
    public class GetAccountsHandler : IRequestHandler<GetAccountsQuery, IEnumerable<AccountDto>>
    {
        private readonly IAccountRepository _repository;
        public GetAccountsHandler(IAccountRepository repository) => _repository = repository;

        public async Task<IEnumerable<AccountDto>> Handle(GetAccountsQuery request, CancellationToken ct)
        {
            var accounts = await _repository.GetAllAsync();

            return accounts.Select(a => new AccountDto(
                a.Id,
                a.Login,
                string.Empty, // пароль наружу не отдаём
                a.Description,
                a.FirstName,
                a.LastName,
                a.Status,
                a.CompletedTasksCount,
                a.LastUsedAtUtc,
                a.ProxyId,
                a.Proxy?.IpAddress,
                a.CreatedAt,
                a.LoginMethod,
                a.PhoneNumber,
                a.SessionInfo,
                a.SessionExpiresAtUtc,
                a.RiskScore));
        }
    }
}
