using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs
{
    public class EntityHub : Hub
    {
        public async Task Subscribe(string group)
            => await Groups.AddToGroupAsync(Context.ConnectionId, group);

        public async Task Unsubscribe(string group)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}
