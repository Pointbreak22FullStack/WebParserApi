using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Infrastructure.Services.Captcha
{
    public sealed class PlaywrightCaptchaSolver
    {
        private const int MaxAttempts = 15;
        private const string CaptchaPrompt =
            "This is a CAPTCHA image. Read the characters exactly as they appear. " +
            "Reply with ONLY the characters — no spaces, no explanation, nothing else.";

        private readonly IAiService _aiService;
        private readonly ILogger<PlaywrightCaptchaSolver> _logger;
        private readonly HttpClient _httpClient = new();

        public PlaywrightCaptchaSolver(IAiService aiService, ILogger<PlaywrightCaptchaSolver> logger)
        {
            _aiService = aiService;
            _logger    = logger;
        }

        public async Task SolveAsync(IPage page, CancellationToken ct = default)
        {
            await page.WaitForTimeoutAsync(3000);

            var captchaDescription = page.Locator("span.Captcha-description");
            if (await captchaDescription.CountAsync() == 0)
            {
                _logger.LogInformation("Captcha not detected, skipping.");
                return;
            }

            _logger.LogInformation("Captcha detected, starting solver...");

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    var solved = await TrySolveAttemptAsync(page, captchaDescription, attempt, ct);
                    if (solved) return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed.", attempt);
                    await RefreshCaptchaAsync(page);
                    await page.WaitForTimeoutAsync(2000);
                }
            }

            _logger.LogError("Failed to solve captcha after {Max} attempts.", MaxAttempts);
        }

        private async Task<bool> TrySolveAttemptAsync(
            IPage page, ILocator captchaDescription, int attempt, CancellationToken ct)
        {
            var captchaImg = page.Locator("img.Captcha-img");
            await captchaImg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            var imgUrl = await captchaImg.GetAttributeAsync("src") ?? string.Empty;
            if (string.IsNullOrEmpty(imgUrl)) throw new InvalidOperationException("Captcha image URL is empty.");

            var imageBytes = await _httpClient.GetByteArrayAsync(imgUrl, ct);
            var text       = await _aiService.CompleteWithImageAsync(imageBytes, CaptchaPrompt, ct);

            _logger.LogInformation("Attempt {Attempt}: AI recognized '{Text}'.", attempt, text);

            if (string.IsNullOrEmpty(text) || text.Length < 3)
            {
                _logger.LogInformation("Recognition too short, refreshing captcha.");
                await RefreshCaptchaAsync(page);
                return false;
            }

            var input = page.Locator("[data-testid='captcha-input'] input[data-testid='text-field-input']");
            await input.FillAsync(string.Empty);
            await input.FillAsync(text);
            await Task.Delay(1000, ct);

            await page.Locator("button[data-testid='captcha-send']").ClickAsync();
            await page.WaitForTimeoutAsync(3000);

            if (await captchaDescription.CountAsync() == 0)
            {
                _logger.LogInformation("Captcha solved on attempt {Attempt}.", attempt);
                return true;
            }

            _logger.LogInformation("Wrong answer on attempt {Attempt}, retrying.", attempt);
            return false;
        }

        private async Task RefreshCaptchaAsync(IPage page)
        {
            var refreshBtn = page.Locator("[data-testid='captcha-refresh']");
            if (await refreshBtn.IsVisibleAsync())
            {
                await refreshBtn.ClickAsync();
                await page.WaitForTimeoutAsync(2000);
            }
        }
    }
}
