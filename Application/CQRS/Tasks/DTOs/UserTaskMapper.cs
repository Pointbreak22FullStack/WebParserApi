using Domain.Entities;
using Domain.Enums;

namespace Application.CQRS.Tasks.DTOs
{
    public static class UserTaskMapper
    {
        public static UserTaskDto ToDto(UserTask task)
        {
            var accounts = task.TaskAccounts
                .Select(ta => new TaskAccountDto(
                    ta.AccountId,
                    ta.Account?.Login,
                    ta.Account?.Status ?? AccountStatus.Active,
                    ta.Status,
                    ta.CompletedAt,
                    ta.DurationSeconds))
                .ToList();

            var result = new TaskResultSummaryDto(
                accounts.Count(a => a.Status == TaskAccountStatus.Completed),
                accounts.Count(a => a.Status is TaskAccountStatus.InQueue
                                               or TaskAccountStatus.Active
                                               or TaskAccountStatus.InWork),
                accounts.Count(a => a.Status == TaskAccountStatus.Error));

            int? durationSeconds = null;
            if (task.StartedAt.HasValue)
            {
                var end = task.CompletedAt ?? DateTime.UtcNow;
                durationSeconds = (int)(end - task.StartedAt.Value).TotalSeconds;
            }

            var history = task.Logs
                .OrderBy(l => l.Timestamp)
                .Select(l => new TaskLogDto(l.Timestamp, l.Message, MapLevel(l.Level)))
                .ToList();

            return new UserTaskDto(
                task.Id,
                task.Type,
                task.TargetUrl,
                task.Status,
                task.Progress,
                accounts,
                result,
                durationSeconds,
                task.CreatedAt,
                history);
        }

        private static string MapLevel(Domain.Enums.LogLevel level) => level switch
        {
            Domain.Enums.LogLevel.Info    => "info",
            Domain.Enums.LogLevel.Warning => "warn",
            Domain.Enums.LogLevel.Error   => "error",
            Domain.Enums.LogLevel.Success => "success",
            _                             => "info"
        };
    }
}
