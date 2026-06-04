using Domain.Enums;

namespace Application.CQRS.Tasks.DTOs
{
    public record TaskAccountDto(
        int AccountId,
        string? AccountLogin,
        AccountStatus AccountStatus,
        TaskAccountStatus Status,
        DateTime? CompletedAt,
        int? DurationSeconds
    );
}
