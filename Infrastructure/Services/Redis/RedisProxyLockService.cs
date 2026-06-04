using Application.Common.Interfaces;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Infrastructure.Services.Redis
{
    /// <summary>
    /// Распределённый лок прокси через Redis SET NX.
    /// Гарантирует, что один прокси одновременно используется только одним аккаунтом/задачей.
    /// </summary>
    public class RedisProxyLockService : IProxyLockService
    {
        private readonly IDatabase _redis;
        private const string Prefix = "proxy_lock:";

        private readonly ConcurrentDictionary<int, string> _held = new();

        public RedisProxyLockService(IConnectionMultiplexer mux)
            => _redis = mux.GetDatabase();

        public async Task<bool> TryAcquireAsync(int proxyId, TimeSpan ttl, CancellationToken ct = default)
        {
            var value = Guid.NewGuid().ToString("N");
            var acquired = await _redis.LockTakeAsync(Key(proxyId), value, ttl);
            if (acquired)
                _held[proxyId] = value;
            return acquired;
        }

        public async Task ReleaseAsync(int proxyId, CancellationToken ct = default)
        {
            if (_held.TryRemove(proxyId, out var value))
                await _redis.LockReleaseAsync(Key(proxyId), value);
        }

        public async Task ForceReleaseAsync(int proxyId, CancellationToken ct = default)
        {
            _held.TryRemove(proxyId, out _);
            await _redis.KeyDeleteAsync(Key(proxyId));
        }

        private static string Key(int proxyId) => $"{Prefix}{proxyId}";
    }
}
