using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Services.Parser.Authorize;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Processors
{
    internal static class OtvetMailReportHelper
    {
        /// <summary>
        /// Жалуется на элемент (пост или ответ) по его DOM-ID.
        /// Последовательность: три точки → Пожаловаться → рандомная причина → Отправить.
        /// Возвращает true при успешной отправке.
        /// </summary>
        public static async Task<bool> ReportAsync(
            IPage page,
            string targetId,
            DelayPreset delay,
            ITaskLogger logger,
            CancellationToken ct)
        {
            await logger.InfoAsync($"Жду загрузки элемента #{targetId}...");
            await page.WaitForSelectorAsync(
                $"[id='{targetId}']",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            // Кнопка «три точки» (MoreHorizontal24) в шапке элемента
            var moreButtonSel = $"[id='{targetId}'] button:has(svg[data-icon='MoreHorizontal24'])";
            await logger.InfoAsync("Ищу кнопку меню (три точки)...");
            await page.WaitForSelectorAsync(moreButtonSel, new PageWaitForSelectorOptions { Timeout = 10_000 });
            await page.ClickAsync(moreButtonSel);

            await new ActionDelay(delay).WaitAsync(ct);

            // Ждём появления выпадающего листа и кликаем «Пожаловаться»
            var actionSheetSel = "[class*='_ActionSheet__contentWrapper_']";
            await logger.InfoAsync("Жду появления меню...");
            await page.WaitForSelectorAsync(actionSheetSel, new PageWaitForSelectorOptions { Timeout = 8_000 });

            var reportItemSel = $"{actionSheetSel} [role='button']:has-text('Пожаловаться')";
            await page.WaitForSelectorAsync(reportItemSel, new PageWaitForSelectorOptions { Timeout = 5_000 });
            await page.ClickAsync(reportItemSel);

            await new ActionDelay(delay).WaitAsync(ct);

            // Ждём открытия модального окна жалобы
            var modalSel = "[class*='_ModalCardInnerContainer_']";
            await logger.InfoAsync("Жду открытия окна жалобы...");
            await page.WaitForSelectorAsync(modalSel, new PageWaitForSelectorOptions { Timeout = 8_000 });

            // Собираем все пункты причины, исключаем последний («Мне не нравится»)
            var cells = await page.QuerySelectorAllAsync($"{modalSel} [class*='_Cell_m4ttk_']");
            if (cells.Count == 0)
            {
                await logger.ErrorAsync("Не найдено ни одного пункта жалобы.");
                return false;
            }

            var validCells = cells.Count > 1 ? cells.Take(cells.Count - 1).ToList() : cells.ToList();
            var randomIndex = Random.Shared.Next(validCells.Count);
            var selectedCell = validCells[randomIndex];

            // Читаем название причины из атрибута id radio-input
            var reasonInput = await selectedCell.QuerySelectorAsync("input[type='radio']");
            var reasonName  = await reasonInput?.GetAttributeAsync("id") ?? (randomIndex + 1).ToString();

            await logger.InfoAsync($"Выбираю причину жалобы \"{reasonName}\" (пункт {randomIndex + 1} из {validCells.Count})...");

            // Кликаем label внутри ячейки — он активирует radio-input
            var cellLabel = await selectedCell.QuerySelectorAsync("label[class*='_SimpleCell_']");
            if (cellLabel is not null)
                await cellLabel.ClickAsync();
            else
                await selectedCell.ClickAsync();

            await new ActionDelay(delay).WaitAsync(ct);

            // Ждём активации кнопки «Отправить» и кликаем
            await logger.InfoAsync("Нажимаю «Отправить»...");
            var submitSel = $"{modalSel} button[class*='_ButtonSubmit_']:not([disabled])";
            await page.WaitForSelectorAsync(submitSel, new PageWaitForSelectorOptions { Timeout = 8_000 });
            await page.ClickAsync(submitSel);

            return true;
        }
    }
}
