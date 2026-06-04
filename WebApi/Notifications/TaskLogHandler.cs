using Application.Common.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Notifications
{
    public class TaskLogHandler : INotificationHandler<TaskLogNotification>
    {
        private readonly IHubContext<EntityHub> _hub;
        public TaskLogHandler(IHubContext<EntityHub> hub) => _hub = hub;

        public Task Handle(TaskLogNotification notification, CancellationToken ct)
            => _hub.Clients.Group(notification.Group)
                   .SendAsync("onTaskLog", notification.Entry, ct);
    }
}
