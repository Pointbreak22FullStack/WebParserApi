using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface ITaskRepository
    {
        /// <param name="userId">Если задан — только задачи этого пользователя; null — все задачи.</param>
        Task<IReadOnlyList<UserTask>> GetAllAsync(string? userId = null, CancellationToken ct = default);
        Task<UserTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
