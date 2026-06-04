using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ProxyRepository : IProxyRepository
    {
        private readonly ApplicationDbContext _context;
        public ProxyRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Proxy>> GetAllAsync()
        {
            return await _context.Proxies
                .Include(p => p.Accounts)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Proxy?> GetByIdAsync(int id) => await _context.Proxies.FindAsync(id);

        public async Task<Proxy?> GetByIdWithAccountsAsync(int id)
            => await _context.Proxies.Include(p => p.Accounts).FirstOrDefaultAsync(p => p.Id == id);

        public async Task AddAsync(Proxy proxy)
        {
            await _context.Proxies.AddAsync(proxy);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Proxy proxy)
        {
            _context.Proxies.Update(proxy);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var proxy = await _context.Proxies.FindAsync(id);
            if (proxy != null)
            {
                _context.Proxies.Remove(proxy);
                await _context.SaveChangesAsync();
            }
        }
    }
}
