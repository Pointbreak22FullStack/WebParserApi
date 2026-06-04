using Application.Common.Interfaces;
using Application.Common.Notifications;
using Application.CQRS.Tasks.DTOs;
using Domain.Entities;
using Hangfire;
using MediatR;

namespace Application.CQRS.Tasks.Commands
{
    public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublisher _publisher;

        public CreateTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _publisher = publisher;
        }

        public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken ct)
        {
            var task = new UserTask(
                _currentUserService.UserId, request.Type, request.TargetUrl,
                request.Count, request.Delay,
                request.CommentText, request.MaybeTargetUrl, request.PromotionAfterPost,
                request.NotifyOnComplete, request.NotifyOnError, request.NotifyTelegram);

            await _unitOfWork.Tasks.AddAsync(task);
            await _unitOfWork.SaveChangesAsync(ct);

            await _publisher.Publish(
                new EntityChangedNotification("tasks", ChangeType.Created, UserTaskMapper.ToDto(task)), ct);

            BackgroundJob.Enqueue<ITaskExecutor>(x => x.ExecuteAsync(task.Id, CancellationToken.None));
            return task.Id;
        }
    }
}
