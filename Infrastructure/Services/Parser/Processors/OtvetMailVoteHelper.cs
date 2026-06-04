using Application.Common.Interfaces;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Infrastructure.Services.Parser.Processors
{
    /// <summary>
    /// Вспомогательный класс для голосования (лайк / дизлайк) на otvet.mail.ru.
    /// Поддерживает два вида URL:
    ///   • /question/239179382              — голосование за пост
    ///   • /question/239179382?reply=XXXXX  — голосование за ответ / вложенный ответ
    ///
    /// В обоих случаях структура кнопки одинакова: один &lt;button&gt; содержит обе стрелки.
    ///   UpVote   → span c классом «_Button_iconBeforeAppearance*» → нажато: «...Positive»
    ///   DownVote → span c классом «_Button_iconAfterAppearance*»  → нажато: «...Negative»
    /// </summary>
    internal static class OtvetMailVoteHelper
    {
        // CSS-классы стрелок (CSS-модули)
        private const string UpVoteClass   = "_UpVote_1yil5_4";
        private const string DownVoteClass = "_DownVote_1yil5_5";

        // Фрагменты класса для проверки «уже нажато»
        // UpVote   ─ span с «iconBefore», DownVote ─ span с «iconAfter»
        private const string UpPressedFragment   = "_Button_iconBeforeAppearancePositive_";
        private const string DownPressedFragment = "_Button_iconAfterAppearanceNegative_";

        private static readonly Regex PostIdRegex  = new(@"/question/(\d+)",  RegexOptions.Compiled);
        private static readonly Regex ReplyIdRegex = new(@"[?&]reply=(\d+)", RegexOptions.Compiled);

        // ─── Публичный API ───────────────────────────────────────────────

        /// <summary>
        /// Разбирает URL, возвращает (postId, replyId).
        /// Если URL содержит reply=, replyId != null и именно он является целевым элементом.
        /// </summary>
        public static (string? postId, string? replyId) ParseUrl(string url)
        {
            var pm = PostIdRegex.Match(url);
            if (!pm.Success) return (null, null);

            var rm = ReplyIdRegex.Match(url);
            return (pm.Groups[1].Value, rm.Success ? rm.Groups[1].Value : null);
        }

        /// <summary>
        /// Ставит голос (▲ или ▼) за элемент с указанным ID (пост или ответ).
        /// Возвращает true — проголосовано, false — уже было.
        /// </summary>
        public static async Task<bool> VoteAsync(
            IPage page,
            string targetId,
            bool upvote,
            ITaskLogger logger,
            CancellationToken ct)
        {
            var dirClass      = upvote ? UpVoteClass          : DownVoteClass;
            var pressedFrag   = upvote ? UpPressedFragment     : DownPressedFragment;
            var arrowLabel    = upvote ? "лайка (▲)"           : "дизлайка (▼)";

            // [id='...'] безопаснее #..., когда ID начинается с цифры
            var clickSel   = $"[id='{targetId}'] div[class*='{dirClass}']";
            var pressedSel = $"[id='{targetId}'] span[class*='{pressedFrag}']";

            await logger.InfoAsync($"Жду загрузки элемента #{targetId}...");
            await page.WaitForSelectorAsync(
                $"[id='{targetId}']",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            await logger.InfoAsync($"Ищу кнопку {arrowLabel}...");
            await page.WaitForSelectorAsync(clickSel, new PageWaitForSelectorOptions { Timeout = 10_000 });

            // Голос уже поставлен?
            if (await page.QuerySelectorAsync(pressedSel) is not null)
            {
                await logger.InfoAsync("Голос уже поставлен ранее, пропускаю.");
                return false;
            }

            // Кликаем по div стрелки — событие всплывает до родительской <button>
            await page.ClickAsync(clickSel);

            // Ждём смены класса как подтверждение
            await page.WaitForSelectorAsync(pressedSel, new PageWaitForSelectorOptions { Timeout = 8_000 });
            return true;
        }
    }
}
