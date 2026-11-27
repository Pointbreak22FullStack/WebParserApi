namespace Application.Common.Notifications
{
    /// <summary>Имена групп SignalR, общие для хаба и публикующего кода.</summary>
    public static class HubGroups
    {
        public const string Accounts = "accounts";
        public const string Proxies  = "proxies";
        public const string Tasks    = "tasks";

        /// <summary>Все задачи (для администраторов).</summary>
        public const string AllTasks = "tasks:all";

        /// <summary>Задачи одного пользователя.</summary>
        public static string UserTasks(string userId) => $"tasks:{userId}";

        /// <summary>Живые логи задач одного пользователя.</summary>
        public static string UserLogs(string userId) => $"user-logs:{userId}";
    }
}
