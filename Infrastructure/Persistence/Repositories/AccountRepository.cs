using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;
        public AccountRepository(ApplicationDbContext context) => _context = context;

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Account?> GetByIdAsync(int id) => await _context.Accounts.FindAsync(id);

        public async Task<Account?> GetByIdWithProxyAsync(int id)
            => await _context.Accounts.Include(a => a.Proxy).FirstOrDefaultAsync(a => a.Id == id);

        public async Task<Account?> GetAvailableAccountAsync()
        {
            return await _context.Accounts
                .Where(a => a.Status == AccountStatus.Active)
                .OrderBy(a => a.CompletedTasksCount) // least-used first
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Account>> GetAccountsByStatusAsync(AccountStatus status)
        {
            return await _context.Accounts
                .Where(a => a.Status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<Account>> GetActiveAccountsWithProxyAsync()
        {
            return await _context.Accounts
                .Include(a => a.Proxy)
                .Where(a => a.Status == AccountStatus.Active)
                .OrderBy(a => a.CompletedTasksCount)
                .ToListAsync();
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            return await _context.Accounts
                .Include(a => a.Proxy)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
