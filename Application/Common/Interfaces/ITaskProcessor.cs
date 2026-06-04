using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces
{
    public interface ITaskProcessor
    {
        TaskType Type { get; }
        Task ProcessAsync(UserTask task, CancellationToken ct);
    }
}
