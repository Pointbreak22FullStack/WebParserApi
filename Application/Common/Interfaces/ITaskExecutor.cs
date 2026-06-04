namespace Application.Common.Interfaces
{
    public interface ITaskExecutor
    {
        Task ExecuteAsync(Guid taskId, CancellationToken ct);
    }
}
