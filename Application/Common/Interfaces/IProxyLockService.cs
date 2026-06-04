namespace Application.Common.Interfaces
{
    public interface IProxyLockService
    {
        /// <summary>Захватить эксклюзивный лок на прокси. True — лок успешно взят этим процессом.</summary>
        Task<bool> TryAcquireAsync(int proxyId, TimeSpan ttl, CancellationToken ct = default);

        /// <summary>Снять лок, захваченный этим процессом.</summary>
        Task ReleaseAsync(int proxyId, CancellationToken ct = default);

        /// <summary>Принудительно удалить лок из Redis (используется при старте для сброса зависших прокси).</summary>
        Task ForceReleaseAsync(int proxyId, CancellationToken ct = default);
    }
}
