using Domain.Enums;

namespace Application.CQRS.Tasks.DTOs
{
    public record UserTaskDto(
        Guid Id,
        TaskType Type,
        string TargetUrl,
        UserTaskStatus Status,
        int Progress,
        IReadOnlyList<TaskAccountDto> Accounts,
        TaskResultSummaryDto Result,
        int? DurationSeconds,
        DateTime CreatedAt,
        IReadOnlyList<TaskLogDto> History
    );

    public record TaskResultSummaryDto(
        int CompletedCount,
        int InProgressCount,
        int ErrorCount
    );

    public record TaskLogDto(
        DateTime Time,
        string Message,
        string Level
    );
}
