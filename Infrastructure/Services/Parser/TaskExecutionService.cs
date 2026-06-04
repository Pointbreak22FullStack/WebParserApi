using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Tasks.DTOs;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Parser
{
    public class TaskExecutionService : ITaskExecutor
    {
        private readonly IEnumerable<ITaskProcessor> _processors;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskRepository _taskRepository;
        private readonly IPublisher _publisher;
        private readonly ILogger<TaskExecutionService> _logger;

        public TaskExecutionService(
            IEnumerable<ITaskProcessor> processors,
            IUnitOfWork unitOfWork,
            ITaskRepository taskRepository,
            IPublisher publisher,
            ILogger<TaskExecutionService> logger)
        {
            _processors = processors;
            _unitOfWork = unitOfWork;
            _taskRepository = taskRepository;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid taskId, CancellationToken ct)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
            if (task == null) return;

            var processor = _processors.FirstOrDefault(p => p.Type == task.Type)
                ?? throw new InvalidOperationException($"No processor registered for task type {task.Type}");

            task.Status = UserTaskStatus.Processing;
            task.StartedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            await PublishUpdatedAsync(taskId, ct);

            try
            {
                await processor.ProcessAsync(task, ct);
                task.Status = UserTaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;

                if (task.NotifyOnComplete) { /* TODO: SignalR / email notification */ }
                if (task.NotifyTelegram)   { /* TODO: Telegram notification */ }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Task {TaskId}] Выполнение завершилось с ошибкой.", taskId);
                task.Status = UserTaskStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                if (task.NotifyOnError) { /* TODO: error notification */ }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            await PublishUpdatedAsync(taskId, ct);
        }

        private async Task PublishUpdatedAsync(Guid taskId, CancellationToken ct)
        {
            var full = await _taskRepository.GetByIdAsync(taskId, ct);
            if (full == null) return;

            await _publisher.Publish(
                new EntityChangedNotification("tasks", ChangeType.Updated, UserTaskMapper.ToDto(full)), ct);
        }
    }
}
