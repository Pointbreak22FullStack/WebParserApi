using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces
{
    public interface IAccountRepository
    {
        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account?> GetByIdAsync(int id);
        Task<Account?> GetByIdWithProxyAsync(int id);
        Task<Account?> GetAvailableAccountAsync();
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
        Task DeleteAsync(int id);
        Task<IEnumerable<Account>> GetAccountsByStatusAsync(AccountStatus status);
        Task<IEnumerable<Account>> GetActiveAccountsWithProxyAsync();
    }
}
