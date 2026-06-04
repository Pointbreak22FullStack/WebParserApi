using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Processors
{
    public class CommentTaskProcessor : ITaskProcessor
    {
        private readonly AccountActivator _activator;

        public TaskType Type => TaskType.Comment;

        public CommentTaskProcessor(AccountActivator activator) => _activator = activator;

        public Task ProcessAsync(UserTask task, CancellationToken ct)
            => _activator.RunAsync(task, (account, context, logger, ct) => CommentAsync(task, context, logger, ct), ct);

        private static async Task CommentAsync(UserTask task, IBrowserContext context, ITaskLogger logger, CancellationToken ct)
        {
            // Страница уже открыта на task.TargetUrl после авторизации.
            // Используем первую страницу контекста вместо открытия новой.
            var page = context.Pages.Count > 0
                ? context.Pages[0]
                : await context.NewPageAsync();

            await logger.InfoAsync("Оставляю комментарий...");

            // TODO: найти поле комментария и кнопку отправки для конкретного сайта
            await page.FillAsync("textarea[name='comment']", task.CommentText ?? "");
            await page.ClickAsync("button#submit");

            if (task.PromotionAfterPost)
            {
                // TODO: логика накрутки лайков после публикации комментария
            }

            await logger.SuccessAsync("Комментарий оставлен.");
        }
    }
}
