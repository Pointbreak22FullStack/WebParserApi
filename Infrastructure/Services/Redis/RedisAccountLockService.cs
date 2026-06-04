using Application.Common.Interfaces;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Infrastructure.Services.Redis
{
    /// <summary>
    /// Распределённый лок аккаунтов через Redis SET NX.
    /// Синглтон — хранит словарь lockValue чтобы только этот процесс снял свой лок.
    /// </summary>
    public class RedisAccountLockService : IAccountLockService
    {
        private readonly IDatabase _redis;
        private const string Prefix = "account_lock:";

        // lockValue уникален на каждый захват — не позволяет другому процессу снять чужой лок
        private readonly ConcurrentDictionary<int, string> _held = new();

        public RedisAccountLockService(IConnectionMultiplexer mux)
            => _redis = mux.GetDatabase();

        public async Task<bool> TryAcquireAsync(int accountId, TimeSpan ttl, CancellationToken ct = default)
        {
            var value = Guid.NewGuid().ToString("N");
            var acquired = await _redis.LockTakeAsync(Key(accountId), value, ttl);
            if (acquired)
                _held[accountId] = value;
            return acquired;
        }

        public async Task ReleaseAsync(int accountId, CancellationToken ct = default)
        {
            if (_held.TryRemove(accountId, out var value))
                await _redis.LockReleaseAsync(Key(accountId), value);
        }

        public async Task RefreshAsync(int accountId, TimeSpan ttl, CancellationToken ct = default)
        {
            if (_held.TryGetValue(accountId, out var value))
                await _redis.LockExtendAsync(Key(accountId), value, ttl);
        }

        public async Task ForceReleaseAsync(int accountId, CancellationToken ct = default)
        {
            _held.TryRemove(accountId, out _);
            await _redis.KeyDeleteAsync(Key(accountId));
        }

        private static string Key(int accountId) => $"{Prefix}{accountId}";
    }
}
