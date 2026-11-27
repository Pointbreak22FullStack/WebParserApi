using Application.Common.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Notifications
{
    public class EntityChangedHandler : INotificationHandler<EntityChangedNotification>
    {
        private readonly IHubContext<EntityHub> _hub;
        public EntityChangedHandler(IHubContext<EntityHub> hub) => _hub = hub;

        public Task Handle(EntityChangedNotification notification, CancellationToken ct)
        {
            var clients = notification.OwnerUserId is { } ownerId
                ? _hub.Clients.Groups(HubGroups.UserTasks(ownerId), HubGroups.AllTasks)
                : _hub.Clients.Group(notification.Group);

            return clients.SendAsync("onEntityChanged", notification, ct);
        }
    }
}
