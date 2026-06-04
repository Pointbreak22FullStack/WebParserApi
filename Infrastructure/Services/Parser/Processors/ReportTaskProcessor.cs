using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Processors
{
    public class ReportTaskProcessor : ITaskProcessor
    {
        private readonly AccountActivator _activator;

        public TaskType Type => TaskType.Report;

        public ReportTaskProcessor(AccountActivator activator) => _activator = activator;

        public Task ProcessAsync(UserTask task, CancellationToken ct)
            => _activator.RunAsync(task, (account, context, logger, ct) => ReportAsync(task, context, logger, ct), ct);

        private static async Task ReportAsync(UserTask task, IBrowserContext context, ITaskLogger logger, CancellationToken ct)
        {
            var page = context.Pages.Count > 0
                ? context.Pages[0]
                : await context.NewPageAsync();

            var (postId, replyId) = OtvetMailVoteHelper.ParseUrl(task.TargetUrl);
            if (postId is null)
            {
                await logger.ErrorAsync($"Не удалось извлечь ID поста из URL: {task.TargetUrl}");
                return;
            }

            // Если URL содержит ?reply=, жалуемся на ответ; иначе — на пост
            var targetId = replyId ?? postId;

            var reported = await OtvetMailReportHelper.ReportAsync(page, targetId, task.Delay, logger, ct);

            await (reported
                ? logger.SuccessAsync("Жалоба успешно отправлена.")
                : logger.ErrorAsync("Не удалось отправить жалобу."));
        }
    }
}
