using Application.Common.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Notifications
{
    /// <summary>Тост в интерфейсе о завершении задачи.</summary>
    public class TaskNotificationHandler : INotificationHandler<TaskNotification>
    {
        private readonly IHubContext<EntityHub> _hub;
        public TaskNotificationHandler(IHubContext<EntityHub> hub) => _hub = hub;

        // Clients.User адресует все подключения пользователя (по claim NameIdentifier / sub)
        public Task Handle(TaskNotification notification, CancellationToken ct)
            => notification.ShowToast
                ? _hub.Clients.User(notification.UserId).SendAsync("onTaskNotification", notification, ct)
                : Task.CompletedTask;
    }
}
