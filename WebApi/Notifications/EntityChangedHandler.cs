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

        public async Task Handle(EntityChangedNotification notification, CancellationToken ct)
            => await _hub.Clients.Group(notification.Group)
                .SendAsync("onEntityChanged", notification, ct);
    }
}
