using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Tasks.DTOs;
using Domain.Entities;
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
        private readonly ITaskCancellationRegistry _cancellation;
        private readonly IPublisher _publisher;
        private readonly ILogger<TaskExecutionService> _logger;

        public TaskExecutionService(
            IEnumerable<ITaskProcessor> processors,
            IUnitOfWork unitOfWork,
            ITaskRepository taskRepository,
            ITaskCancellationRegistry cancellation,
            IPublisher publisher,
            ILogger<TaskExecutionService> logger)
        {
            _processors = processors;
            _unitOfWork = unitOfWork;
            _taskRepository = taskRepository;
            _cancellation = cancellation;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid taskId, CancellationToken ct)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
            if (task == null) return;

            // Отменена пользователем до старта
            if (task.Status == UserTaskStatus.Canceled)
            {
                _logger.LogInformation("[Task {TaskId}] Отменена до запуска, пропускаю.", taskId);
                return;
            }

            var processor = _processors.FirstOrDefault(p => p.Type == task.Type)
                ?? throw new InvalidOperationException($"No processor registered for task type {task.Type}");

            using var cts = _cancellation.Register(taskId, ct);
            try
            {
                task.Status = UserTaskStatus.Processing;
                task.StartedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                await PublishUpdatedAsync(taskId);

                try
                {
                    await processor.ProcessAsync(task, cts.Token);

                    // Обработчики аккаунтов перехватывают исключения, поэтому отмену проверяем явно
                    task.Status = cts.IsCancellationRequested ? UserTaskStatus.Canceled : UserTaskStatus.Completed;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    task.Status = UserTaskStatus.Canceled;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Task {TaskId}] Выполнение завершилось с ошибкой.", taskId);
                    task.Status = UserTaskStatus.Failed;
                }

                task.CompletedAt = DateTime.UtcNow;
                if (task.Status == UserTaskStatus.Canceled)
                    _logger.LogInformation("[Task {TaskId}] Отменена пользователем.", taskId);

                // Сохраняем итог даже при отмене — поэтому CancellationToken.None
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                await PublishUpdatedAsync(taskId);
                await NotifyUserAsync(task);
            }
            finally
            {
                _cancellation.Unregister(taskId);
            }
        }

        private Task NotifyUserAsync(UserTask task)
        {
            var (level, message, toast) = task.Status switch
            {
                UserTaskStatus.Completed => ("success", "Задача выполнена.", task.NotifyOnComplete),
                UserTaskStatus.Failed    => ("error", "Задача завершилась с ошибкой. Подробности в логах.", task.NotifyOnError),
                UserTaskStatus.Canceled  => ("info", "Задача отменена.", task.NotifyOnComplete),
                _                        => ("info", string.Empty, false),
            };

            if (!toast && !task.NotifyTelegram)
                return Task.CompletedTask;

            return _publisher.Publish(
                new TaskNotification(task.UserId, task.Id, level, message, toast, task.NotifyTelegram),
                CancellationToken.None);
        }

        private async Task PublishUpdatedAsync(Guid taskId)
        {
            var full = await _taskRepository.GetByIdAsync(taskId, CancellationToken.None);
            if (full == null) return;

            await _publisher.Publish(
                new EntityChangedNotification(HubGroups.Tasks, ChangeType.Updated, UserTaskMapper.ToDto(full), full.UserId),
                CancellationToken.None);
        }
    }
}
