using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Infrastructure.Services.Parser
{
    /// <summary>
    /// Проверяет доступность прокси перед использованием через TCP-соединение.
    /// TCP connect работает для обоих типов прокси (HTTP и SOCKS5) без дополнительных библиотек.
    /// </summary>
    public sealed class ProxyTester
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);

        private readonly ILogger<ProxyTester> _logger;

        public ProxyTester(ILogger<ProxyTester> logger)
            => _logger = logger;

        public async Task<bool> TestAsync(Proxy proxy, CancellationToken ct = default)
        {
            try
            {
                using var tcp = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(ConnectTimeout);

                await tcp.ConnectAsync(proxy.IpAddress, proxy.Port, cts.Token);

                _logger.LogInformation("[ProxyTester] {Ip}:{Port} ({Type}) — ОК.",
                    proxy.IpAddress, proxy.Port, proxy.Type);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[ProxyTester] {Ip}:{Port} ({Type}) недоступен: {Msg}.",
                    proxy.IpAddress, proxy.Port, proxy.Type, ex.Message);
                return false;
            }
        }
    }
}
