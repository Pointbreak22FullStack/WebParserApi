using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<UserTask> Tasks { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
