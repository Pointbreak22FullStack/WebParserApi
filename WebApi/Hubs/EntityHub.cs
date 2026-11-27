using Application.Common.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebApi.Hubs
{
    /// <summary>
    /// Живые обновления сущностей и логов. Подписаться можно только на разрешённые группы:
    /// accounts, proxies, tasks (свои задачи; администратор получает все) и user-logs:{свой id}.
    /// </summary>
    [Authorize]
    public class EntityHub : Hub
    {
        public const string AdminRole = "Admin";

        public Task Subscribe(string group)
            => Groups.AddToGroupAsync(Context.ConnectionId, ResolveGroup(group));

        public Task Unsubscribe(string group)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, ResolveGroup(group));

        private string ResolveGroup(string group)
        {
            var userId = Context.UserIdentifier
                ?? throw new HubException("Не удалось определить пользователя.");

            if (group == HubGroups.Accounts || group == HubGroups.Proxies)
                return group;

            if (group == HubGroups.Tasks)
                return IsAdmin(Context.User)
                    ? HubGroups.AllTasks
                    : HubGroups.UserTasks(userId);

            if (group == HubGroups.UserLogs(userId))
                return group;

            throw new HubException($"Подписка на группу '{group}' запрещена.");
        }

        // Роль может прийти как "role" или как ClaimTypes.Role (зависит от маппинга claim-ов)
        private static bool IsAdmin(ClaimsPrincipal? user)
            => user?.Claims.Any(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == AdminRole) == true;
    }
}
