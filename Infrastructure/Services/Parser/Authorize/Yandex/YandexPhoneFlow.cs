using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services.Captcha;
using Infrastructure.Services.Parser.Authorize;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Authorize.Yandex
{
    public class YandexPhoneFlow : ILoginFlow
    {
        private readonly PlaywrightCaptchaSolver  _captchaSolver;
        private readonly ILogger<YandexPhoneFlow> _logger;

        public LoginMethod Method => LoginMethod.YandexPhone;

        public YandexPhoneFlow(PlaywrightCaptchaSolver captchaSolver, ILogger<YandexPhoneFlow> logger)
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
                _logger.LogInformation("[YandexPhone] Сессия действительна, логин пропущен.");
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
            await logger.InfoAsync("Сессия недействительна, выполняю вход по телефону...");
            logger.SetProgress(10);

            var page = await YandexHelper.OpenAuthPageAsync(context, _logger);
            await logger.InfoAsync("Страница авторизации открыта.");
            logger.SetProgress(15);

            // ── Шаг 3: ввод телефона ──────────────────────────────────────────────
            if (await YandexHelper.IsPhoneOnlyFormAsync(page))
            {
                // page-split-add-user: форма с прямым вводом телефона (иностранный прокси)
                await logger.InfoAsync("Форма ввода телефона (иностранный прокси) — ввожу номер...");
                await EnterPhoneOnSplitFormAsync(page, account, delay, ct);
                await logger.InfoAsync("Номер введён.");
                logger.SetProgress(25);

                // page-code: ввод кода из пуш-уведомления
                await logger.WaitingAsync("Ожидаю код из пуш-уведомления...");
                logger.SetProgress(30);
                await WaitForCodePageAsync(page);

                var splitCode = await getCodeAsync();
                await logger.SuccessAsync("Код получен, ввожу...");
                logger.SetProgress(40);

                await EnterCodeAsync(page, splitCode, delay, ct);
                await logger.SuccessAsync("Код введён.");
                logger.SetProgress(50);

                // page-suggest: выбор аккаунта из списка
                await logger.InfoAsync("Выбираю аккаунт...");
                logger.SetProgress(55);
                await SelectAccountFromSuggestPageAsync(page, account, getCodeAsync, delay, logger, ct);
                logger.SetProgress(60);

                // TODO: реализовать флоу после выбора аккаунта
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                return;
            }

            await SwitchToPhoneTabAsync(page);
            await logger.InfoAsync("Переключился на вход по телефону.");

            await EnterPhoneAsync(page, account, delay, ct);
            await logger.InfoAsync("Номер телефона введён.");
            logger.SetProgress(25);

            // ── Шаг 4: вводим SMS-код ─────────────────────────────────────────────
            await logger.WaitingAsync("Ожидаю SMS-код...");
            logger.SetProgress(30);
            var code = await getCodeAsync();
            await logger.SuccessAsync("SMS-код получен, ввожу...");
            logger.SetProgress(40);

            await EnterCodeAsync(page, code, delay, ct);
            await logger.SuccessAsync("Код введён.");
            logger.SetProgress(50);

            // ── Шаг 5: выбираем аккаунт ──────────────────────────────────────────
            await SelectAccountAsync(page, account, getCodeAsync, delay, logger, ct);

            // ── Шаг 6: верификация аккаунта ───────────────────────────────────────
            var verified = await YandexHelper.VerifyPassportLoginAsync(page, account, _logger);
            if (!verified)
                throw new InvalidOperationException(
                    $"Вход выполнен под неверным аккаунтом или страница подтверждения не открылась. " +
                    $"Ожидался: {account.Login}");

            await logger.SuccessAsync("Авторизация успешна.");
            _logger.LogInformation("[YandexPhone] Авторизация успешна.");
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

        private static async Task SwitchToPhoneTabAsync(IPage page)
        {
            var phoneLabel = page.Locator("label:has(input[data-testid='add-user-phone-option'])");
            await phoneLabel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await phoneLabel.ClickAsync();
        }

        // Стандартная форма (page-add-user, после переключения на таб телефона)
        private async Task EnterPhoneAsync(IPage page, Account account, ActionDelay delay, CancellationToken ct)
        {
            var phoneInput = page.Locator("input[data-testid='text-field-input']");
            await phoneInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await delay.WaitAsync(ct);

            await phoneInput.FillAsync(account.PhoneNumber);
            await page.Locator("span:has-text('Далее')").ClickAsync();
            _logger.LogInformation("[YandexPhone] Телефон введён.");
        }

        // page-split-add-user: прямой ввод телефона без переключения табов
        private async Task EnterPhoneOnSplitFormAsync(IPage page, Account account, ActionDelay delay, CancellationToken ct)
        {
            var phoneInput = page.Locator("[data-testid='phone-input'] input[data-testid='text-field-input']");
            await phoneInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await delay.WaitAsync(ct);

            await phoneInput.FillAsync(account.PhoneNumber);
            _logger.LogInformation("[YandexPhone] Телефон введён в split-форму.");

            // Кнопка «Войти» появляется после ввода корректного номера
            var nextBtn = page.Locator("button[data-testid='split-add-user-next-phone']");
            await nextBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await nextBtn.ClickAsync();
            _logger.LogInformation("[YandexPhone] Нажата кнопка 'Войти' (split-форма).");
        }

        // Ожидание страницы ввода кода (page-code) после отправки номера телефона
        private static async Task WaitForCodePageAsync(IPage page)
        {
            await page.Locator("[data-testid='page-code']")
                      .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        }

        // page-suggest: сверяет аккаунты в списке с account.Login, кликает совпавший.
        // Если совпадения нет — ищет по имени и фамилии с вводом SMS-кода.
        private async Task SelectAccountFromSuggestPageAsync(
            IPage page, Account account, Func<Task<string>> getCodeAsync,
            ActionDelay delay, ITaskLogger logger, CancellationToken ct)
        {
            await page.Locator("[data-testid='page-suggest']")
                      .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });

            var items = page.Locator(".AccountSuggestList-list-item");
            int count = await items.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var item      = items.Nth(i);
                var ariaLabel = await item.GetAttributeAsync("aria-label") ?? string.Empty;
                var innerText = await item.InnerTextAsync();

                if (YandexHelper.Matches(ariaLabel, account) || YandexHelper.Matches(innerText, account))
                {
                    await item.ClickAsync();
                    await logger.SuccessAsync($"Аккаунт '{account.Login}' выбран.");
                    _logger.LogInformation("[YandexPhone] Аккаунт {Login} выбран (split-форма).", account.Login);
                    return;
                }
            }

            // Аккаунт не найден — ищем через "Найти по имени и фамилии"
            _logger.LogWarning("[YandexPhone] Аккаунт {Login} не найден — ищу по имени/фамилии.", account.Login);
            await logger.InfoAsync($"Аккаунт '{account.Login}' не найден — нажимаю 'Нет нужного аккаунта'...");

            var noAccountBtn = page.Locator("[data-testid='suggest-do-not-see-popup-btn']");
            await noAccountBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });
            await noAccountBtn.ClickAsync();

            var findByNameOption = page.Locator("[data-testid='menu-option-snf-another-fln']");
            await findByNameOption.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });
            await findByNameOption.ClickAsync();
            _logger.LogInformation("[YandexPhone] Нажато 'Найти по имени и фамилии'.");
            await logger.InfoAsync("Нажато 'Найти по имени и фамилии', жду страницу кода...");

            // page-code: Яндекс отправляет SMS, но иногда с задержкой или ошибкой.
            // Запускаем ожидание кода заранее, параллельно ждём активации кнопки "Отправить повторно".
            // Если код не пришёл к моменту активации кнопки — нажимаем её для повторной отправки.
            var codeTask = getCodeAsync();

            await page.Locator("[data-testid='page-code']")
                      .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

            await logger.WaitingAsync("Ожидаю SMS-код (или активацию кнопки повторной отправки)...");

            await page.WaitForFunctionAsync(
                "() => !document.querySelector(\"[data-testid='code-resend-button']\")?.disabled",
                null, new PageWaitForFunctionOptions { Timeout = 120_000 });

            if (!codeTask.IsCompleted)
            {
                _logger.LogInformation("[YandexPhone] Код ещё не получен — нажимаю 'Отправить код повторно'.");
                await logger.InfoAsync("Нажимаю 'Отправить код повторно'...");
                await page.Locator("[data-testid='code-resend-button']").ClickAsync();
            }

            await logger.WaitingAsync("Ожидаю SMS-код...");
            var code = await codeTask;
            await logger.SuccessAsync("Код получен, ввожу...");

            await EnterCodeAsync(page, code, delay, ct);
            await logger.SuccessAsync("Код введён.");
            _logger.LogInformation("[YandexPhone] SMS-код для поиска по имени введён.");


            Thread.Sleep(100000);
        }

        private async Task EnterCodeAsync(IPage page, string code, ActionDelay delay, CancellationToken ct)
        {
            _logger.LogInformation("[YandexPhone] Ввожу код: {Code}", code);

            var segments = page.Locator("input[data-testid='code-field-segment']");
            await segments.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Task.Delay(200, ct);   // небольшой UI-синхронизационный пауза
            await delay.WaitAsync(ct);
            await segments.First.ClickAsync();
            await page.Keyboard.TypeAsync(code, new() { Delay = 200 });
        }

        private async Task SelectAccountAsync(
            IPage page, Account account, Func<Task<string>> getCodeAsync, ActionDelay delay, ITaskLogger logger, CancellationToken ct)
        {
            _logger.LogInformation("[YandexPhone] Ищу аккаунт: {Login}...", account.Login);
            await logger.InfoAsync("Выбираю аккаунт из списка...");

            await WaitForSuggestPageAsync(page);

            if (await TryClickAccountInListAsync(page, account))
            {
                await logger.SuccessAsync($"Аккаунт '{account.Login}' выбран.");
            }
            else
            {
                _logger.LogWarning("[YandexPhone] Аккаунт {Login} не найден, пробую поиск по имени/фамилии...", account.Login);
                await logger.InfoAsync("Аккаунт не найден в списке, ищу по имени/фамилии...");
                await FindAccountByNameAsync(page, account, getCodeAsync, delay, logger, ct);
            }
        }

        private static async Task WaitForSuggestPageAsync(IPage page)
        {
            var header = page.Locator("h1", new() { HasTextString = "Выберите аккаунт" });
            await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20000 });
        }

        private static async Task<bool> TryClickAccountInListAsync(IPage page, Account account)
        {
            var items = page.Locator(".AccountSuggestList-list-item");
            int count = await items.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var item      = items.Nth(i);
                var ariaLabel = await item.GetAttributeAsync("aria-label") ?? string.Empty;
                var innerText = await item.InnerTextAsync();

                if (YandexHelper.Matches(ariaLabel, account) || YandexHelper.Matches(innerText, account))
                {
                    await item.ClickAsync();
                    return true;
                }
            }

            return false;
        }

        private async Task FindAccountByNameAsync(
            IPage page, Account account, Func<Task<string>> getCodeAsync, ActionDelay delay, ITaskLogger logger, CancellationToken ct)
        {
            // Открываем поиск по имени/фамилии
            var noAccountBtn = page.Locator("[data-testid='suggest-do-not-see-popup-btn']");
            await noAccountBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await noAccountBtn.ClickAsync();

            var findByNameOption = page.Locator("[data-testid='menu-option-snf-another-fln']");
            await findByNameOption.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await Task.Delay(5000, ct);
            await findByNameOption.ClickAsync();

            await _captchaSolver.SolveAsync(page, ct);

            // Вводим дополнительный код подтверждения
            var segments = page.Locator("input[data-testid='code-field-segment']");
            await segments.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20000 });

            await logger.WaitingAsync("Ожидаю дополнительный код для поиска по имени...");
            var additionalCode = await getCodeAsync();
            await logger.SuccessAsync("Дополнительный код получен.");
            _logger.LogInformation("[YandexPhone] Ввожу код (поиск по имени): {Code}", additionalCode);
            await delay.WaitAsync(ct);
            await segments.First.ClickAsync();
            await page.Keyboard.TypeAsync(additionalCode, new() { Delay = 200 });

            // Заполняем форму имени и фамилии
            var flnForm = page.Locator("[data-testid='page-fln']");
            await flnForm.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20000 });

            var firstNameInput = page.Locator("[data-testid='fln-input-firstName'] input[data-testid='text-field-input']");
            await firstNameInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await delay.WaitAsync(ct);
            await firstNameInput.FillAsync(account.FirstName ?? string.Empty);

            var lastNameInput = page.Locator("[data-testid='fln-input-lastName'] input[data-testid='text-field-input']");
            await delay.WaitAsync(ct);
            await lastNameInput.FillAsync(account.LastName ?? string.Empty);

            await page.WaitForFunctionAsync(
                "() => !document.querySelector(\"[data-testid='fln-next']\")?.disabled", null,
                new() { Timeout = 10000 });
            await page.Locator("[data-testid='fln-next']").ClickAsync();

            // Ищем аккаунт в обновлённом списке
            await WaitForSuggestPageAsync(page);

            var items = page.Locator(".AccountSuggestList-list-item");
            int count = await items.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var item      = items.Nth(i);
                var ariaLabel = await item.GetAttributeAsync("aria-label") ?? string.Empty;
                var innerText = await item.InnerTextAsync();

                if (YandexHelper.Matches(ariaLabel, account) || YandexHelper.Matches(innerText, account))
                {
                    await item.ClickAsync();
                    await logger.SuccessAsync($"Аккаунт '{account.Login}' найден и выбран.");
                    _logger.LogInformation("[YandexPhone] Аккаунт {Login} найден и выбран.", account.Login);
                    return;
                }
            }

            await logger.ErrorAsync($"Аккаунт '{account.Login}' не найден даже после поиска по имени.");
            _logger.LogError("[YandexPhone] Аккаунт {Login} не найден даже после поиска по имени.", account.Login);
        }
    }
}
