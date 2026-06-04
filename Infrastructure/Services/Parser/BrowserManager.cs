using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser
{
    // Singleton — один браузер на всё приложение, несколько контекстов параллельно.
    // Подключается к запущенному Docker-контейнеру Camoufox по WebSocket.
    public class BrowserManager : IAsyncDisposable
    {
        // Убирает Яндекс AdFox рекламу через CSS и MutationObserver.
        // [id^="adfox_"] — специфичный селектор только для Яндекс AdFox, не трогает остальное.
        private const string AdBlockScript = """
            (function () {
                const style = document.createElement('style');
                style.textContent = '[id^="adfox_"]{display:none!important;visibility:hidden!important}';
                (document.head || document.documentElement).appendChild(style);

                new MutationObserver(function (mutations) {
                    for (var i = 0; i < mutations.length; i++) {
                        var nodes = mutations[i].addedNodes;
                        for (var j = 0; j < nodes.length; j++) {
                            var node = nodes[j];
                            if (node.nodeType !== 1) continue;
                            if (node.id && node.id.indexOf('adfox_') === 0) {
                                node.remove();
                                continue;
                            }
                            if (node.querySelectorAll) {
                                node.querySelectorAll('[id^="adfox_"]').forEach(function (el) { el.remove(); });
                            }
                        }
                    }
                }).observe(document.documentElement, { childList: true, subtree: true });
            })();
            """;

        private IPlaywright? _playwright;
        private IBrowser?    _browser;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly string _camoufoxHttpUrl;
        private readonly ILogger<BrowserManager> _logger;

        public BrowserManager(IOptions<CamoufoxOptions> options, ILogger<BrowserManager> logger)
        {
            _camoufoxHttpUrl = options.Value.HttpUrl;
            _logger          = logger;
        }

        private async Task<IBrowser> EnsureBrowserAsync()
        {
            if (_browser is { IsConnected: true }) return _browser;

            await _initLock.WaitAsync();
            try
            {
                if (_browser is { IsConnected: true }) return _browser;

                if (_browser != null)
                {
                    try { await _browser.CloseAsync(); } catch { }
                    _browser = null;
                }

                _playwright ??= await Playwright.CreateAsync();
                var wsUrl = await PollForWsUrlAsync();
                _browser = await _playwright.Firefox.ConnectAsync(wsUrl);
                _logger.LogInformation("[BrowserManager] Подключён к Camoufox.");
                return _browser;
            }
            finally
            {
                _initLock.Release();
            }
        }

        // Ждёт, пока контейнер поднимет браузер и вернёт ws:// URL.
        private async Task<string> PollForWsUrlAsync()
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    var response = await http.GetStringAsync(_camoufoxHttpUrl);
                    if (response.StartsWith("ws://") || response.StartsWith("wss://"))
                    {
                        _logger.LogInformation("[BrowserManager] Camoufox WebSocket: {Url}", response);
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("[BrowserManager] Camoufox не готов ({Attempt}/15): {Msg}", i + 1, ex.Message);
                }
                await Task.Delay(2000);
            }
            throw new InvalidOperationException(
                $"Camoufox не ответил по {_camoufoxHttpUrl} за 30 секунд.");
        }

        /// <summary>Создаёт изолированный браузерный контекст (аккаунт + опциональные прокси и сессия).</summary>
        public async Task<IBrowserContext> CreateContextAsync(
            Domain.Entities.Proxy? proxy = null,
            string? storageState = null,
            CancellationToken ct = default)
        {
            var browser = await EnsureBrowserAsync();

            var contextOptions = new BrowserNewContextOptions
            {
                // UserAgent не задаём — Camoufox подставляет реалистичный Firefox UA самостоятельно.
                Locale       = "ru-RU",
                TimezoneId   = "Europe/Moscow",
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
            };

            if (!string.IsNullOrEmpty(storageState))
                contextOptions.StorageState = storageState;

            if (proxy != null)
            {
                var scheme = proxy.Type == ProxyType.Socks5 ? "socks5" : "http";
                contextOptions.Proxy = new Microsoft.Playwright.Proxy
                {
                    Server   = $"{scheme}://{proxy.IpAddress}:{proxy.Port}",
                    Username = proxy.Login,
                    Password = proxy.Password
                };
            }

            var context = await browser.NewContextAsync(contextOptions);
            await context.AddInitScriptAsync(AdBlockScript);
            return context;
        }

        /// <summary>Открывает страницу в новом контексте и возвращает её (для ручного использования).</summary>
        public async Task<IPage> CreatePageAsync(string url, Domain.Entities.Proxy? proxy = null)
        {
            var context = await CreateContextAsync(proxy);
            var page    = await context.NewPageAsync();
            await page.GotoAsync(url);
            return page;
        }

        public async ValueTask DisposeAsync()
        {
            try { if (_browser != null) await _browser.CloseAsync(); } catch { }
            _playwright?.Dispose();
        }
    }
}
