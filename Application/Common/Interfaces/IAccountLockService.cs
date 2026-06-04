namespace Application.Common.Interfaces
{
    public interface IAccountLockService
    {
        /// <summary>
        /// Попытаться взять эксклюзивный лок на аккаунт.
        /// Возвращает true, если лок успешно захвачен этим процессом.
        /// </summary>
        Task<bool> TryAcquireAsync(int accountId, TimeSpan ttl, CancellationToken ct = default);

        /// <summary>Снять лок, захваченный этим процессом.</summary>
        Task ReleaseAsync(int accountId, CancellationToken ct = default);

        /// <summary>Продлить TTL лока (для долгих задач, чтобы не протух раньше времени).</summary>
        Task RefreshAsync(int accountId, TimeSpan ttl, CancellationToken ct = default);

        /// <summary>Принудительно удалить лок из Redis (используется при старте для сброса зависших аккаунтов).</summary>
        Task ForceReleaseAsync(int accountId, CancellationToken ct = default);
    }
}
