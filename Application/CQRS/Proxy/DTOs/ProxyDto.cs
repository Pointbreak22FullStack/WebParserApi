namespace Application.CQRS.Proxy.DTOs
{
    public record ProxyDto(
        int Id,
        string IpAddress,
        int Port,
        string Login,
        string Password,
        Domain.Enums.ProxyType Type,
        bool IsActive,
        int? LatencyMs,
        int AccountsCount,
        bool HasError,
        bool IsWarming,
        bool IsFree);
}
