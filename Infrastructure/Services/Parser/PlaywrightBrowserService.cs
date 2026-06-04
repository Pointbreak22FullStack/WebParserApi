using Application.Common.Interfaces;

namespace Infrastructure.Services.Parser
{
    public class PlaywrightBrowserService : IBrowserService
    {
        private readonly BrowserManager _browserManager;

        public PlaywrightBrowserService(BrowserManager browserManager)
            => _browserManager = browserManager;

        public async Task OpenPageAsync(string url, CancellationToken ct)
        {
            var page = await _browserManager.CreatePageAsync(url);

            var closeTcs = new TaskCompletionSource<bool>();
            page.Close += (_, _) => closeTcs.TrySetResult(true);
            await closeTcs.Task;
        }
    }
}
