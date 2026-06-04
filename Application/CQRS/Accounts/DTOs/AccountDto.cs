using Domain.Enums;

namespace Application.CQRS.Accounts.DTOs
{
    public record AccountDto(
        int Id,
        string Login,
        string Password,
        string? Description,
        string? FirstName,
        string? LastName,
        AccountStatus Status,
        int CompletedTasksCount,
        DateTime? LastUsedAtUtc,
        int? ProxyId,
        string? ProxyIp,
        DateTime CreatedAt,
        LoginMethod LoginMethod,
        string PhoneNumber,
        string SessionInfo,
        DateTime? SessionExpiresAtUtc,
        int RiskScore);
}
