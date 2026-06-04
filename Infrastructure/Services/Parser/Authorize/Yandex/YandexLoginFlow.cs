using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services.Captcha;
using Infrastructure.Services.Parser.Authorize;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Authorize.Yandex
{
    public class YandexLoginFlow : ILoginFlow
    {
        private readonly PlaywrightCaptchaSolver  _captchaSolver;
        private readonly ILogger<YandexLoginFlow> _logger;

        public LoginMethod Method => LoginMethod.YandexMail;

        public YandexLoginFlow(PlaywrightCaptchaSolver captchaSolver, ILogger<YandexLoginFlow> logger)
        {
            _captchaSolver = captchaSolver;
            _logger        = logger;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Main flow
        // ─────────────────────────────────────────────────────────────────────────

        public async Task LoginAsync(
            IBrowserContext    context,
            Account            account,
            string             targetUrl,
            Func<Task<string>> getCodeAsync,
            ActionDelay        delay,
            ITaskLogger        logger,
            CancellationToken  ct)
        {
            await logger.InfoAsync("Проверка сессии...");
            logger.SetProgress(5);

            // ── Шаг 1: проверяем сессию ───────────────────────────────────────────
            // TryGetValidSessionPageAsync возвращает открытую страницу (на id.yandex.ru),
            // чтобы переход в targetUrl происходил прямо из неё — в том же контексте.
            var sessionPage = await YandexHelper.TryGetValidSessionPageAsync(context, _logger);
            if (sessionPage != null)
            {
                _logger.LogInformation("[YandexMail] Сессия действительна, логин пропущен.");
                await logger.SuccessAsync("Сессия активна — вход пропущен.");
                logger.SetProgress(65);

                account.SessionInfo         = await context.StorageStateAsync();
                account.SessionExpiresAtUtc = SessionHelper.ExtractExpiresAt(account.SessionInfo);

                await logger.InfoAsync("Открываю целевой URL...");
                await YandexHelper.FinalizeAsync(sessionPage, account, targetUrl, _logger);
                await logger.SuccessAsync("Целевая страница открыта.");
                logger.SetProgress(90);
                return;
            }

            // ── Шаг 2: открываем страницу авторизации ────────────────────────────
            await logger.InfoAsync("Сессия недействительна, выполняю вход...");
            logger.SetProgress(10);

            var page = await YandexHelper.OpenAuthPageAsync(context, _logger);
            await logger.InfoAsync("Страница авторизации открыта.");
            logger.SetProgress(15);

            // ── Шаг 3: вводим логин ───────────────────────────────────────────────
            await EnterMailAsync(page, account, delay, ct);
            await logger.InfoAsync($"Логин '{account.Login}' введён.");
            logger.SetProgress(20);

            await _captchaSolver.SolveAsync(page, ct);

            if (!await IsOnPasswordPageAsync(page))
            {
                await logger.WaitingAsync("Ожидаю пуш-код...");
                await EnterPushCodeIfRequestedAsync(page, getCodeAsync, delay, ct);
                await logger.SuccessAsync("Пуш-код введён.");
                logger.SetProgress(35);
            }

            // ── Шаг 4: вводим пароль ─────────────────────────────────────────────
            if (!await IsAuthorizedAsync(page))
            {
                await logger.InfoAsync("Ввожу пароль...");
                await EnterPasswordAsync(page, account, delay, ct);
                logger.SetProgress(40);

                // Проверяем, не появилась ли ошибка «Неверный пароль»
                if (await IsPasswordWrongAsync(page))
                {
                    _logger.LogWarning("[YandexMail] Неверный пароль для '{Login}' — запускаю восстановление.", account.Login);
                    await logger.InfoAsync("Неверный пароль — запускаю восстановление пароля...");
                    await YandexPasswordRecovery.RecoverAsync(context, page, account, targetUrl, getCodeAsync, delay, logger, _logger, ct);
                    return;   // дальнейший флоу (сессия, targetUrl) берёт на себя RecoverAsync
                }

                await logger.SuccessAsync("Пароль введён.");
                await DismissChangePasswordInfoAsync(page);
                await _captchaSolver.SolveAsync(page, ct);
                await RequestPhoneCodeAsync(page);
            }

            // ── Шаг 5: телефонное подтверждение (SMS / push) ─────────────────────
            await logger.WaitingAsync("Обработка телефонного подтверждения...");
            await TryHandlePhoneChallengeAsync(page, getCodeAsync, delay, logger, ct);

            await TryHandleAccountSuggestAsync(page, account);
            await TryDismissWebAuthnRegistrationAsync(page);

            // ── Шаг 6: верификация аккаунта ───────────────────────────────────────
            var verified = await YandexHelper.VerifyPassportLoginAsync(page, account, _logger);
            if (!verified)
                throw new InvalidOperationException(
                    $"Вход выполнен под неверным аккаунтом или страница подтверждения не открылась. " +
                    $"Ожидался: {account.Login}");

            await logger.SuccessAsync("Авторизация успешна.");
            _logger.LogInformation("[YandexMail] Авторизация успешна.");
            logger.SetProgress(65);

            // ── Шаг 7: сохраняем сессию passport.yandex.ru ───────────────────────
            // Сессия сохраняется здесь — пока страница ещё на id.yandex.ru.
            // FinalizeAsync переходит на targetUrl и сессию не перезаписывает.
            account.SessionInfo         = await context.StorageStateAsync();
            account.SessionExpiresAtUtc = SessionHelper.ExtractExpiresAt(account.SessionInfo);
            await logger.SuccessAsync("Сессия passport сохранена.");
            logger.SetProgress(70);

            // ── Шаг 8: переходим на targetUrl и выполняем OAuth-вход ─────────────
            await logger.InfoAsync("Открываю целевой URL...");
            await YandexHelper.FinalizeAsync(page, account, targetUrl, _logger);
            await logger.SuccessAsync("Целевая страница открыта.");
            logger.SetProgress(85);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Проверяет, появилось ли сообщение об ошибке «Неверный пароль»
        /// (элемент <c>span[data-testid='error-message']</c>).
        /// </summary>
        private static async Task<bool> IsPasswordWrongAsync(IPage page)
        {
            try
            {
                await page.Locator("span[data-testid='error-message']")
                          .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
                return true;
            }
            catch { return false; }
        }

        private static async Task<bool> IsAuthorizedAsync(IPage page)
        {
            try
            {
                await page.Locator("[data-testid='user-info-section']")
                          .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 2000 });
                return true;
            }
            catch { return false; }
        }

        private static async Task<bool> IsOnPasswordPageAsync(IPage page)
        {
            try
            {
                await page.Locator("[data-testid='password-input'] input[data-testid='text-field-input']")
                          .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
                return true;
            }
            catch { return false; }
        }

        private async Task EnterMailAsync(IPage page, Account account, ActionDelay delay, CancellationToken ct)
        {
            var input = page.Locator("[data-testid='add-user-login-input'] input[data-testid='text-field-input']");
            await input.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await delay.WaitAsync(ct);

            await input.FillAsync(account.Login);
            await page.Locator("button[data-testid='add-user-next']").ClickAsync();
        }

        private async Task EnterPushCodeIfRequestedAsync(IPage page, Func<Task<string>> getCodeAsync, ActionDelay delay, CancellationToken ct)
        {
            try
            {
                var segments = page.Locator("[data-testid='push-code-input'] input[data-testid='code-field-segment']");
                await segments.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                _logger.LogInformation("[YandexMail] Ожидаю пуш-код...");
                var code = await getCodeAsync();
                _logger.LogInformation("[YandexMail] Ввожу пуш-код: {Code}", code);

                await delay.WaitAsync(ct);
                await segments.First.ClickAsync();
                await page.Keyboard.TypeAsync(code, new() { Delay = 200 });
                await page.Locator("button[data-testid='page-push-code-submit-btn']").ClickAsync();
            }
            catch { }
        }

        private async Task EnterPasswordAsync(IPage page, Account account, ActionDelay delay, CancellationToken ct)
        {
            var input = page.Locator("[data-testid='password-input'] input[data-testid='text-field-input']");
            await input.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await delay.WaitAsync(ct);

            await input.FillAsync(account.Password);
            await page.Locator("button[data-testid='password-next']").ClickAsync();
            _logger.LogInformation("[YandexMail] Пароль введён.");
        }

        private async Task DismissChangePasswordInfoAsync(IPage page)
        {
            try
            {
                var btn = page.Locator("button[data-testid='page-change-password-info-button']");
                await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await btn.ClickAsync();
                _logger.LogInformation("[YandexMail] Закрыл анкету безопасности.");
            }
            catch { }
        }

        private async Task RequestPhoneCodeAsync(IPage page)
        {
            try
            {
                var btn = page.Locator("button[data-testid='page-change-password-phone-submit']");
                await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await btn.ClickAsync();
                _logger.LogInformation("[YandexMail] Запрошен код подтверждения телефона.");
            }
            catch { }
        }

        /// <summary>
        /// Регистрирует ожидание кода заранее (<c>codeTask</c>) — SMS может прийти пока браузер
        /// нажимает кнопки и ждёт навигации. Затем последовательно обрабатывает:
        /// пуш-экран → запрос SMS → стартовый экран «Безопасный вход» → форму ввода SMS-кода.
        /// </summary>
        private async Task TryHandlePhoneChallengeAsync(IPage page, Func<Task<string>> getCodeAsync, ActionDelay delay, ITaskLogger logger, CancellationToken ct)
        {
            // Регистрируем ожидание кода заранее — SMS может прийти пока браузер
            // нажимает кнопки и ждёт навигации. earlyBuffer подхватит её сразу.
            var codeTask = getCodeAsync();

            // Шаг 1: пуш-экран — нажимаем «Отправить код sms», ждём пока скроется
            try
            {
                var pushPage = page.Locator("[data-testid='page-challenge-with-push']");
                await pushPage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                _logger.LogInformation("[YandexMail] Экран пуш-кода — нажимаю 'Отправить код sms'...");
                await logger.InfoAsync("Запрашиваю SMS-код вместо пуш...");

                var smsBtn = page.Locator("[data-testid='code-resend-button']");
                await smsBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
                await smsBtn.ClickAsync();

                try
                {
                    await pushPage.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
                    _logger.LogInformation("[YandexMail] Пуш-экран скрылся.");
                    await logger.InfoAsync("SMS запрошена, жду код...");
                }
                catch
                {
                    _logger.LogWarning("[YandexMail] Пуш-экран не скрылся за 10 сек — продолжаю поиск формы кода.");
                }
            }
            catch
            {
                _logger.LogInformation("[YandexMail] Пуш-экран не появился — проверяю другие флоу.");
            }

            // Шаг 2: стартовый экран «Безопасный вход» — опциональный
            try
            {
                var challengeStart = page.Locator("[data-testid='page-challenge-with-phone-confirmation-start']");
                await challengeStart.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
                _logger.LogInformation("[YandexMail] Экран 'Безопасный вход', нажимаю 'Подтвердить'...");
                await logger.InfoAsync("Экран безопасного входа — подтверждаю телефон...");
                await page.Locator("[data-testid='challenges-phone-confirmation-next']").ClickAsync();
            }
            catch
            {
                _logger.LogInformation("[YandexMail] Стартовый экран не появился — продолжаю.");
            }

            // Шаг 3: форма ввода SMS-кода
            try
            {
                var challengeCode = page.Locator("[data-testid='page-challenge-with-phone-confirmation-code']");
                await challengeCode.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
                _logger.LogInformation("[YandexMail] Форма SMS-кода появилась.");

                await logger.WaitingAsync("Ожидаю SMS-код подтверждения входа...");
                logger.SetProgress(50);
                var code = await codeTask;
                await logger.SuccessAsync("SMS-код получен, ввожу...");
                logger.SetProgress(55);

                var segments = page.Locator(
                    "[data-testid='page-challenge-with-phone-confirmation-code'] input[data-testid='code-field-segment']");
                await segments.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await delay.WaitAsync(ct);
                await segments.First.ClickAsync();
                await page.Keyboard.TypeAsync(code, new() { Delay = 200 });
                _logger.LogInformation("[YandexMail] Код подтверждения введён.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "[YandexMail] TryHandlePhoneChallengeAsync: форма кода не найдена — {Msg}. URL: {Url}",
                    ex.Message, page.Url);
            }
        }

        private async Task TryHandleAccountSuggestAsync(IPage page, Account account)
        {
            try
            {
                var suggestPage = page.Locator("[data-testid='page-suggest-list']");
                await suggestPage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                _logger.LogInformation("[YandexMail] Модалка выбора аккаунта, ищу {Login}...", account.Login);

                await YandexHelper.TryClickAccountAsync(page, account, _logger);
            }
            catch { }
        }

        private async Task TryDismissWebAuthnRegistrationAsync(IPage page)
        {
            try
            {
                var laterBtn = page.Locator("[data-testid='webauthn-reg-later-button']");
                await laterBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await laterBtn.ClickAsync();
                _logger.LogInformation("[YandexMail] Закрыл предложение входа по биометрии.");
            }
            catch { }
        }
    }
}
