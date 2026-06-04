using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface ITaskRepository
    {
        Task<IReadOnlyList<UserTask>> GetAllAsync(CancellationToken ct = default);
        Task<UserTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
