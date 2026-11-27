using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Tasks.DTOs;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.CQRS.Tasks.Commands.CancelTask
{
    public class CancelTaskHandler : IRequestHandler<CancelTaskCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskRepository _taskRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ITaskCancellationRegistry _cancellation;
        private readonly IPublisher _publisher;

        public CancelTaskHandler(
            IUnitOfWork unitOfWork,
            ITaskRepository taskRepository,
            ICurrentUserService currentUser,
            ITaskCancellationRegistry cancellation,
            IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _taskRepository = taskRepository;
            _currentUser = currentUser;
            _cancellation = cancellation;
            _publisher = publisher;
        }

        public async Task<bool> Handle(CancelTaskCommand request, CancellationToken ct)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(request.Id);

            // Чужую задачу не показываем: для пользователя она «не найдена»
            if (task == null || (!_currentUser.IsAdmin && task.UserId != _currentUser.UserId))
                throw new NotFoundException(nameof(UserTask), request.Id);

            switch (task.Status)
            {
                case UserTaskStatus.Pending:
                    // Ещё не стартовала: помечаем, и фоновое задание её пропустит
                    task.Status = UserTaskStatus.Canceled;
                    task.CompletedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(ct);

                    var full = await _taskRepository.GetByIdAsync(task.Id, ct);
                    if (full != null)
                        await _publisher.Publish(new EntityChangedNotification(
                            HubGroups.Tasks, ChangeType.Updated, UserTaskMapper.ToDto(full), full.UserId), ct);
                    return true;

                case UserTaskStatus.Processing:
                    // Статус Canceled выставит TaskExecutionService, когда выполнение остановится
                    return _cancellation.Cancel(task.Id);

                default:
                    return false;
            }
        }
    }
}
