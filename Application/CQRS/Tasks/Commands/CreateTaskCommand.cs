using Domain.Enums;
using MediatR;

namespace Application.CQRS.Tasks.Commands
{
    public record CreateTaskCommand : IRequest<Guid>
    {
        public TaskType Type { get; init; }
        public string TargetUrl { get; init; }
        public string? CommentText { get; init; }
        public string? MaybeTargetUrl { get; init; }
        public bool PromotionAfterPost { get; init; }
        public int Count { get; init; }
        public DelayPreset Delay { get; init; }
        public bool NotifyOnComplete { get; init; }
        public bool NotifyOnError { get; init; }
        public bool NotifyTelegram { get; init; }
    }
}
