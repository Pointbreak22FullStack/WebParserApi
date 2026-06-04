using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IProxyRepository
    {
        Task<Proxy?> GetByIdAsync(int id);
        Task<Proxy?> GetByIdWithAccountsAsync(int id);
        Task<IEnumerable<Proxy>> GetAllAsync();
        Task AddAsync(Proxy proxy);
        Task UpdateAsync(Proxy proxy);
        Task DeleteAsync(int id);
    }
}
