using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Authorize.Yandex
{
    internal static class YandexHelper
    {
        // ── URLs ──────────────────────────────────────────────────────────────────
        private const string AuthUrl       = "https://passport.yandex.ru/auth";
        private const string ProfileUrl    = "https://passport.yandex.ru/profile";
        private const string AuthUrlCom    = "https://passport.yandex.com/auth";
        private const string ProfileUrlCom = "https://passport.yandex.com/profile";

        // ── Timeouts ──────────────────────────────────────────────────────────────
        private const int SessionCheckDelayMs      = 1500;
        private const int AfterClickDelayMs        = 500;
        private const int AuthPageTimeoutMs        = 30_000;
        private const int YandexIdButtonTimeoutMs  = 8_000;
        private const int OAuthUrlTimeoutMs        = 10_000;
        private const int AccountSuggestTimeoutMs  = 5_000;
        private const int NetworkIdleTimeoutMs     = 10_000;
        private const int OAuthPopupCloseTimeoutMs = 30_000;

        private const int LoginModalPopupTimeoutSec = 2;
        private const int OAuthPopupTimeoutSec      = 4;

        // ── OAuth confirm button selectors (tried in order) ───────────────────────
        private static readonly string[] OAuthConfirmSelectors =
        [
            "button[data-type='allow']",
            "[data-testid='allow-button']",
            "button:has-text('Разрешить')",
            "button:has-text('Продолжить')",
            "button:has-text('Войти')",
            "input[type='submit'][value='Разрешить']",
            "input[type='submit'][value='Войти']",
        ];

        // ─────────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Проверяет наличие активной сессии, открывая <c>passport.yandex.ru/profile</c>.
        /// </summary>
        /// <returns>
        /// Открытая страница (на <c>id.yandex.ru</c>), если сессия активна — caller должен
        /// использовать её для дальнейшей навигации (<see cref="FinalizeAsync"/>), чтобы
        /// OAuth-переход проходил в том же браузерном контексте.<br/>
        /// <c>null</c> — сессия истекла или произошла ошибка (страница уже закрыта).
        /// </returns>
        public static async Task<IPage?> TryGetValidSessionPageAsync(IBrowserContext context, ILogger logger)
        {
            var page = await context.NewPageAsync();
            try
            {
                await page.GotoAsync(ProfileUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await Task.Delay(SessionCheckDelayMs);

                var url   = page.Url;
                var valid = !url.Contains("/auth") && !url.Contains("/registration");
                logger.LogInformation("[YandexHelper] Сессия {Result} (url={Url}).", valid ? "активна" : "истекла", url);

                if (valid)
                    return page;   // страница остаётся открытой — caller переходит из неё в targetUrl

                await page.CloseAsync();
                return null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[YandexHelper] Ошибка при проверке сессии.");
                try { await page.CloseAsync(); } catch { }
                return null;
            }
        }

        /// <summary>
        /// Открывает страницу авторизации <c>passport.yandex.ru/auth</c>, настраивает
        /// виртуальный WebAuthn-аутентификатор и ждёт появления формы входа.
        /// Поддерживает обе формы: стандартную (<c>page-add-user</c>) и форму ввода
        /// телефона (<c>page-split-add-user</c>), которая появляется при использовании
        /// прокси из иностранных регионов.
        /// </summary>
        public static async Task<IPage> OpenAuthPageAsync(IBrowserContext context, ILogger logger)
        {
            var page = await context.NewPageAsync();
            logger.LogInformation("[YandexHelper] Открываю страницу авторизации...");

            await page.GotoAsync(AuthUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await SetupWebAuthnAsync(context, page, logger);

            await page.Locator("[data-testid='page-add-user'], [data-testid='page-split-add-user']")
                      .First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = AuthPageTimeoutMs });

            if (await IsPhoneOnlyFormAsync(page))
                logger.LogWarning("[YandexHelper] Обнаружена форма ввода телефона (page-split-add-user) — прокси из иностранного региона.");

            logger.LogInformation("[YandexHelper] Страница авторизации готова.");
            return page;
        }

        /// <summary>
        /// Возвращает <c>true</c>, если на странице отображается форма ввода номера телефона
        /// (<c>page-split-add-user</c>), характерная для иностранных прокси.
        /// </summary>
        public static async Task<bool> IsPhoneOnlyFormAsync(IPage page)
        {
            try   { return await page.Locator("[data-testid='page-split-add-user']").IsVisibleAsync(); }
            catch { return false; }
        }

        /// <summary>
        /// WebAuthn отключён через Firefox-preferences в Camoufox-контейнере.
        /// CDP недоступен для Firefox — виртуальный аутентификатор не нужен.
        /// Остаточные попапы биометрии обрабатываются через TryDismissWebAuthnRegistrationAsync в login-флоу.
        /// </summary>
        public static Task SetupWebAuthnAsync(IBrowserContext context, IPage page, ILogger logger)
        {
            logger.LogInformation("[YandexHelper] WebAuthn: обрабатывается Camoufox через Firefox preferences.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Переходит на <paramref name="targetUrl"/> со страницы активной сессии Yandex и
        /// выполняет OAuth-вход через «Войти» → «Яндекс ID».
        ///
        /// Поддерживает три сценария OAuth:
        /// <list type="number">
        ///   <item>Попап с модалкой входа + попап OAuth.</item>
        ///   <item>Попап с модалкой входа + редирект OAuth на той же странице.</item>
        ///   <item>Редирект OAuth на текущей странице (без попапов).</item>
        /// </list>
        ///
        /// При ошибке <c>unauthorized_client</c> нажимает «Понятно» и ждёт авто-редиректа.
        /// </summary>
        public static async Task FinalizeAsync(IPage page, Account account, string targetUrl, ILogger logger)
        {
            // Ensure the browser visits passport.yandex.ru before targetUrl so the Yandex session
            // is warmed up in the current browser context. This reduces the probability that the
            // OAuth redirect back to passport.yandex.ru (triggered from targetUrl) triggers the
            // phone-number verification form (page-split-add-user) due to a foreign-proxy IP.
            if (!page.Url.Contains("passport.yandex.ru") && !page.Url.Contains("passport.yandex.com") && !page.Url.Contains("id.yandex.ru"))
            {
                logger.LogInformation("[YandexHelper] Инициализирую сессию Яндекс перед переходом на targetUrl...");
                await page.GotoAsync(ProfileUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await Task.Delay(SessionCheckDelayMs);
                logger.LogInformation("[YandexHelper] Сессия Яндекс инициализирована. URL: {Url}", page.Url);
            }

            logger.LogInformation("[YandexHelper] Перехожу на целевой URL: {Url}", targetUrl);
            await page.GotoAsync(targetUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Task.Delay(SessionCheckDelayMs);
            logger.LogInformation("[YandexHelper] Целевая страница загружена. URL: {Url}", page.Url);

            try
            {
                // ── Шаг 1: кнопка «Войти» ─────────────────────────────────────────
                var loginBtn = page.GetByText("Войти", new() { Exact = true });
                if (!await loginBtn.IsVisibleAsync())
                {
                    logger.LogInformation("[YandexHelper] Кнопка 'Войти' не найдена — вход уже выполнен.");
                    return;
                }

                logger.LogInformation("[YandexHelper] Нажимаю 'Войти'...");
                var loginPopupTask = page.WaitForPopupAsync();   // регистрируем ДО клика
                await loginBtn.ClickAsync();
                await Task.Delay(AfterClickDelayMs);

                // ── Шаг 2: модалка входа (popup или текущая страница) ──────────────
                var loginModalPage = await TryGetPopupAsync(loginPopupTask, LoginModalPopupTimeoutSec, logger,
                    "Модалка входа в popup");
                var activePage = loginModalPage ?? page;

                // ── Шаг 3: кнопка «Яндекс ID» ─────────────────────────────────────
                var yandexIdBtn = activePage.GetByText("Яндекс ID");
                await yandexIdBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = YandexIdButtonTimeoutMs });
                logger.LogInformation("[YandexHelper] Нажимаю 'Яндекс ID' (url={Url})...", activePage.Url);

                var oauthPopupTask = activePage.WaitForPopupAsync();   // регистрируем ДО клика
                await yandexIdBtn.ClickAsync();

                // ── Шаг 4: OAuth страница (popup или редирект) ─────────────────────
                var oauthPage   = await TryGetPopupAsync(oauthPopupTask, OAuthPopupTimeoutSec, logger,
                    "OAuth в popup");
                var oauthTarget = oauthPage ?? activePage;

                await HandleYandexOAuthAsync(oauthTarget, account, logger);

                if (oauthPage != null)
                    await WaitForOAuthPopupCloseAsync(oauthPage, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning("[YandexHelper] FinalizeAsync: {Msg}. URL: {Url}", ex.Message, page.Url);
            }

            await WaitForNetworkIdleAsync(page);
            logger.LogInformation("[YandexHelper] Целевая страница готова. URL: {Url}", page.Url);
        }

        /// <summary>
        /// Ждёт появления страницы успешного входа (<c>data-testid="user-info-section"</c>),
        /// читает логин и имя, сверяет с аккаунтом.
        /// </summary>
        /// <returns><c>true</c> — вход подтверждён; <c>false</c> — страница не открылась или аккаунт не совпал.</returns>
        public static async Task<bool> VerifyPassportLoginAsync(IPage page, Account account, ILogger logger, int timeoutMs = 60000)
        {
            try
            {
                await page.Locator("[data-testid='user-info-section']")
                          .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            }
            catch (Exception ex)
            {
                logger.LogWarning("[YandexHelper] Страница подтверждения входа не появилась: {Msg}", ex.Message);
                return false;
            }

            logger.LogInformation("[YandexHelper] Страница успешного входа открыта. URL: {Url}", page.Url);

            try
            {
                var loginText = (await page.Locator("bdi[data-testid='login']").TextContentAsync() ?? string.Empty).Trim();

                if (string.Equals(loginText, account.Login, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("[YandexHelper] ✓ Аккаунт подтверждён: login='{Login}'", loginText);
                    return true;
                }

                // Дополнительная проверка по имени (InnerTextAsync исключает SVG-текст)
                try
                {
                    var nameText = (await page.Locator("[data-testid='profile-card-menu-trigger']").InnerTextAsync() ?? string.Empty).Trim();
                    if (Matches(nameText, account))
                    {
                        logger.LogInformation(
                            "[YandexHelper] ✓ Аккаунт подтверждён по имени: '{Name}' (на стр.='{PageLogin}', ожид.='{Expected}')",
                            nameText, loginText, account.Login);
                        return true;
                    }
                }
                catch { /* имя недоступно — не критично */ }

                logger.LogError(
                    "[YandexHelper] ✗ Несовпадение аккаунта: ожидался '{Expected}', на странице '{Actual}'",
                    account.Login, loginText);
                return false;
            }
            catch (Exception ex)
            {
                // Данные не прочитать, но страница открылась — засчитываем вход
                logger.LogWarning("[YandexHelper] Не удалось прочитать данные со страницы входа (засчитан): {Msg}", ex.Message);
                return true;
            }
        }

        /// <summary>
        /// Ищет аккаунт в списке <c>page-suggest-list</c> и кликает по нему.
        /// Ошибки игнорируются — список может отсутствовать.
        /// </summary>
        public static async Task TryClickAccountAsync(IPage page, Account account, ILogger logger)
        {
            try
            {
                await page.Locator("[data-testid='page-suggest-list']")
                          .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = AccountSuggestTimeoutMs });

                var items = page.Locator(".AccountSuggestList-list-item");
                int count = await items.CountAsync();

                for (int i = 0; i < count; i++)
                {
                    var item      = items.Nth(i);
                    var ariaLabel = await item.GetAttributeAsync("aria-label") ?? string.Empty;
                    var innerText = await item.InnerTextAsync();

                    if (Matches(ariaLabel, account) || Matches(innerText, account))
                    {
                        await item.ClickAsync();
                        logger.LogInformation("[YandexHelper] Аккаунт {Login} выбран.", account.Login);
                        return;
                    }
                }

                logger.LogWarning("[YandexHelper] Аккаунт {Login} не найден в списке.", account.Login);
            }
            catch { }
        }

        /// <summary>
        /// Проверяет, совпадает ли строка <paramref name="value"/> с логином, полным именем
        /// или слитным именем аккаунта (без учёта регистра).
        /// </summary>
        public static bool Matches(string value, Account account)
        {
            var cmp = StringComparison.OrdinalIgnoreCase;
            return value.Contains(account.Login, cmp)
                || value.Contains($"{account.FirstName} {account.LastName}", cmp)
                || value.Contains($"{account.FirstName}{account.LastName}", cmp);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ждёт открытия popup-окна с ограниченным таймаутом.
        /// </summary>
        /// <returns>Открытый popup или <c>null</c>, если он не появился.</returns>
        private static async Task<IPage?> TryGetPopupAsync(
            Task<IPage> popupTask, int timeoutSeconds, ILogger logger, string label)
        {
            try
            {
                var popup = await popupTask.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
                logger.LogInformation("[YandexHelper] {Label}. URL: {Url}", label, popup.Url);
                return popup;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Ждёт закрытия popup OAuth-окна (таймаут <see cref="OAuthPopupCloseTimeoutMs"/> мс).
        /// </summary>
        private static async Task WaitForOAuthPopupCloseAsync(IPage popup, ILogger logger)
        {
            try
            {
                if (popup.IsClosed)
                {
                    logger.LogInformation("[YandexHelper] OAuth popup уже закрыт.");
                    return;
                }

                var closeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                popup.Close += (_, _) => closeTcs.TrySetResult(true);

                await Task.WhenAny(closeTcs.Task, Task.Delay(OAuthPopupCloseTimeoutMs));
                logger.LogInformation("[YandexHelper] OAuth popup закрылся (isClosed={IsClosed}).", popup.IsClosed);
            }
            catch (Exception ex)
            {
                logger.LogWarning("[YandexHelper] Ожидание закрытия OAuth popup: {Msg}", ex.Message);
            }
        }

        /// <summary>Ждёт <c>NetworkIdle</c> на странице; ошибка игнорируется.</summary>
        private static async Task WaitForNetworkIdleAsync(IPage page)
        {
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = NetworkIdleTimeoutMs });
            }
            catch { }
        }

        /// <summary>
        /// Обрабатывает страницу OAuth на стороне Яндекса (<c>oauth.yandex.ru</c> / <c>id.yandex.ru</c>).
        ///
        /// Порядок действий:
        /// <list type="number">
        ///   <item>Ждёт перехода на Yandex-домен.</item>
        ///   <item>Выбирает аккаунт из списка (если он появился).</item>
        ///   <item>Обрабатывает страницу ошибки <c>unauthorized_client</c> или нажимает кнопку подтверждения.</item>
        /// </list>
        /// </summary>
        private static async Task HandleYandexOAuthAsync(IPage oauthPage, Account account, ILogger logger)
        {
            // Ждём перехода на Yandex OAuth / ID страницу
            try
            {
                await oauthPage.WaitForURLAsync(
                    url => url.Contains("oauth.yandex.ru") || url.Contains("id.yandex.ru") || url.Contains("passport.yandex.ru") || url.Contains("passport.yandex.com"),
                    new() { Timeout = OAuthUrlTimeoutMs });
                logger.LogInformation("[YandexHelper] Yandex OAuth страница: {Url}", oauthPage.Url);
            }
            catch
            {
                logger.LogInformation("[YandexHelper] URL не изменился на Yandex OAuth. Текущий: {Url}", oauthPage.Url);
            }

            await TryClickAccountAsync(oauthPage, account, logger);
            await Task.Delay(AfterClickDelayMs);

            if (await IsOAuthErrorPageAsync(oauthPage))
            {
                await HandleOAuthErrorAsync(oauthPage, logger);
                return;
            }

            await TryClickOAuthConfirmAsync(oauthPage, logger);
        }

        /// <summary>
        /// Обрабатывает страницу ошибки <c>unauthorized_client</c>: нажимает «Понятно»
        /// и ждёт авто-редиректа обратно на targetUrl.
        /// </summary>
        private static async Task HandleOAuthErrorAsync(IPage oauthPage, ILogger logger)
        {
            logger.LogWarning(
                "[YandexHelper] OAuth вернул ошибку (unauthorized_client). Нажимаю 'Понятно', жду редиректа. URL: {Url}",
                oauthPage.Url);

            try
            {
                var okBtn = oauthPage.Locator(".ErrorOutbound-button").First;
                if (await okBtn.IsVisibleAsync())
                    await okBtn.ClickAsync();
            }
            catch { }

            try
            {
                await oauthPage.WaitForURLAsync(
                    url => !url.Contains("oauth.yandex.ru") && !url.Contains("id.yandex.ru") && !url.Contains("passport.yandex.com"),
                    new() { Timeout = OAuthUrlTimeoutMs });
                logger.LogInformation("[YandexHelper] После OAuth ошибки редирект на: {Url}", oauthPage.Url);
            }
            catch { }
        }

        /// <summary>
        /// Перебирает <see cref="OAuthConfirmSelectors"/> и кликает по первой видимой кнопке подтверждения.
        /// </summary>
        private static async Task TryClickOAuthConfirmAsync(IPage oauthPage, ILogger logger)
        {
            foreach (var selector in OAuthConfirmSelectors)
            {
                try
                {
                    var btn = oauthPage.Locator(selector).First;
                    if (await btn.IsVisibleAsync())
                    {
                        logger.LogInformation("[YandexHelper] OAuth: нажимаю '{Selector}'. URL: {Url}", selector, oauthPage.Url);
                        await btn.ClickAsync();
                        return;
                    }
                }
                catch { }
            }

            logger.LogInformation("[YandexHelper] OAuth: кнопка подтверждения не найдена (auto-approve?). URL: {Url}", oauthPage.Url);
        }

        private static async Task<bool> IsOAuthErrorPageAsync(IPage page)
        {
            try   { return await page.Locator(".ErrorOutbound-content").IsVisibleAsync(); }
            catch { return false; }
        }
    }
}
