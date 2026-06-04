using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Authorize.Yandex
{
    /// <summary>
    /// Восстановление пароля Yandex — вызывается из <see cref="YandexLoginFlow"/>,
    /// когда страница ввода пароля вернула ошибку «Неверный пароль».
    ///
    /// Точка входа — <see cref="RecoverAsync"/>. Метод принимает управление начиная
    /// с экрана ввода пароля, на котором уже нажата кнопка «Не помню пароль».
    ///
    /// После успешного восстановления метод обязан:
    ///   1. Обновить <c>account.Password</c> новым паролем.
    ///   2. Вызвать <see cref="YandexHelper.VerifyPassportLoginAsync"/> для проверки сессии.
    ///   3. Сохранить сессию: <c>account.SessionInfo = await context.StorageStateAsync()</c>.
    ///   4. Вызвать <see cref="YandexHelper.FinalizeAsync"/> для перехода на targetUrl.
    /// </summary>
    internal static class YandexPasswordRecovery
    {
        private const int ForgotPasswordButtonTimeoutMs = 5_000;
        private const int RestorePhonePageTimeoutMs     = 10_000;
        private const int AfterFillDelayMs              = 500;

        /// <summary>
        /// Запускает процесс восстановления пароля.
        /// Нажимает «Не помню пароль», вводит номер телефона и передаёт управление
        /// дальнейшей логике восстановления.
        /// </summary>
        /// <param name="context">Браузерный контекст — нужен для сохранения сессии после восстановления.</param>
        /// <param name="page">Страница на форме ввода пароля с ошибкой «Неверный пароль».</param>
        /// <param name="account">Аккаунт; после восстановления нужно обновить <c>account.Password</c>.</param>
        /// <param name="targetUrl">Целевой URL, на который нужно перейти после входа.</param>
        /// <param name="getCodeAsync">Делегат для получения SMS-кодов.</param>
        /// <param name="logger">Логгер задачи для отображения прогресса пользователю.</param>
        /// <param name="log">Системный логгер.</param>
        /// <param name="ct">Токен отмены.</param>
        public static async Task RecoverAsync(
            IBrowserContext    context,
            IPage              page,
            Account            account,
            string             targetUrl,
            Func<Task<string>> getCodeAsync,
            ActionDelay        delay,
            ITaskLogger        logger,
            ILogger            log,
            CancellationToken  ct)
        {
            log.LogInformation("[YandexRecovery] Запуск восстановления пароля для '{Login}'.", account.Login);

            // ── Шаг 1: нажимаем «Не помню пароль» ───────────────────────────────
            await ClickForgotPasswordAsync(page);
            await logger.InfoAsync("Нажата кнопка «Не помню пароль».");
            log.LogInformation("[YandexRecovery] Кнопка 'Не помню пароль' нажата. URL: {Url}", page.Url);

            // ── Шаг 2: вводим номер телефона ─────────────────────────────────────
            await EnterRecoveryPhoneAsync(page, account, logger, log, ct);

            // ── ЗАГЛУШКА: ожидание для инспекции страницы ────────────────────────
            // Убрать после написания дальнейшей логики восстановления.
            // Браузер остаётся открытым — можно смотреть вёрстку пока парсер висит.
            // Задача завершится сама когда отменишь её (CancellationToken).
            await logger.WaitingAsync("⏸ Восстановление пароля: ожидание для инспекции страницы...");
            log.LogWarning("[YandexRecovery] ЗАГЛУШКА: ждём отмены задачи для '{Login}'. URL: {Url}", account.Login, page.Url);

            await Task.Delay(Timeout.Infinite, ct);

            // ── Шаг 3: TODO — обработать следующий экран (SMS-код / новый пароль) ─
            //
            //   var code = await getCodeAsync();
            //   // ввести code в форму подтверждения
            //
            //   account.Password = "<новый пароль>";   // обновить модель
            //
            // ── Шаг 4: TODO — верифицировать вход и сохранить сессию ─────────────
            //
            //   var verified = await YandexHelper.VerifyPassportLoginAsync(page, account, log);
            //   if (!verified)
            //       throw new InvalidOperationException("Восстановление пароля: аккаунт не подтверждён.");
            //
            //   account.SessionInfo         = await context.StorageStateAsync();
            //   account.SessionExpiresAtUtc = SessionHelper.ExtractExpiresAt(account.SessionInfo);
            //   await logger.SuccessAsync("Сессия сохранена после восстановления.");
            //
            // ── Шаг 5: TODO — перейти на targetUrl ───────────────────────────────
            //
            //   await YandexHelper.FinalizeAsync(page, account, targetUrl, log);
            //   await logger.SuccessAsync("Целевая страница открыта.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        private static async Task ClickForgotPasswordAsync(IPage page)
        {
            var btn = page.Locator("button[data-testid='password-restore']");
            await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = ForgotPasswordButtonTimeoutMs });
            await btn.ClickAsync();
        }

        /// <summary>
        /// Ждёт страницу <c>page-restore-phone</c>, заполняет поле телефона полным номером
        /// (включая код страны) и нажимает «Продолжить».
        /// Тройной клик перед заполнением гарантирует замену любого предзаполненного значения.
        /// </summary>
        private static async Task EnterRecoveryPhoneAsync(
            IPage page, Account account, ITaskLogger logger, ILogger log, CancellationToken ct)
        {
            var restorePage = page.Locator("[data-testid='page-restore-phone']");
            await restorePage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = RestorePhonePageTimeoutMs });
            log.LogInformation("[YandexRecovery] Страница ввода телефона открыта. URL: {Url}", page.Url);
            await logger.InfoAsync("Ввожу номер телефона для восстановления...");

            var phoneInput = page.Locator("[data-testid='phone-input'] input[data-testid='text-field-input']");
            await phoneInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            // Тройной клик выделяет весь текст (в т.ч. предзаполненный код страны),
            // FillAsync затем полностью заменяет значение.
            await phoneInput.ClickAsync(new() { ClickCount = 3 });
            await phoneInput.FillAsync(account.PhoneNumber);

            log.LogInformation("[YandexRecovery] Номер '{Phone}' введён.", account.PhoneNumber);
            await Task.Delay(AfterFillDelayMs, ct);

            await page.Locator("button[data-testid='page-restore-phone-submit']").ClickAsync();
            await logger.InfoAsync("Номер телефона введён, нажал «Продолжить».");
            log.LogInformation("[YandexRecovery] Нажал 'Продолжить'. URL: {Url}", page.Url);
        }
    }
}
