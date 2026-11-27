using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Notifications;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.CQRS.Tasks.Commands.DeleteTask
{
    public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IPublisher _publisher;

        public DeleteTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _publisher = publisher;
        }

        public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken ct)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(request.Id);
            if (task == null || (!_currentUser.IsAdmin && task.UserId != _currentUser.UserId))
                throw new NotFoundException(nameof(UserTask), request.Id);

            if (task.Status is UserTaskStatus.Pending or UserTaskStatus.Processing)
                return false;

            var ownerId = task.UserId;
            _unitOfWork.Tasks.Delete(task); // логи и аккаунты задачи удаляются каскадно
            await _unitOfWork.SaveChangesAsync(ct);

            await _publisher.Publish(
                new EntityChangedNotification(HubGroups.Tasks, ChangeType.Deleted, request.Id, ownerId), ct);
            return true;
        }
    }
}
