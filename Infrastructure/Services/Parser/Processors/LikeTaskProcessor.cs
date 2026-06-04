using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Processors
{
    public class LikeTaskProcessor : ITaskProcessor
    {
        private readonly AccountActivator _activator;

        public TaskType Type => TaskType.Like;

        public LikeTaskProcessor(AccountActivator activator) => _activator = activator;

        public Task ProcessAsync(UserTask task, CancellationToken ct)
            => _activator.RunAsync(task, (account, context, logger, ct) => LikeAsync(task, context, logger, ct), ct);

        private static async Task LikeAsync(UserTask task, IBrowserContext context, ITaskLogger logger, CancellationToken ct)
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

            // Если URL содержит ?reply=, голосуем за ответ; иначе — за пост
            var targetId = replyId ?? postId;

            var voted = await OtvetMailVoteHelper.VoteAsync(page, targetId, upvote: true, logger, ct);

            await (voted
                ? logger.SuccessAsync("Лайк поставлен.")
                : logger.SuccessAsync("Лайк уже был поставлен ранее."));
        }
    }
}
