using Domain.Enums;

namespace Domain.Entities
{
    public class UserTask
    {
        public Guid Id { get; private set; }
        public string UserId { get; private set; }
        public TaskType Type { get; private set; }
        public string TargetUrl { get; private set; }
        public string? CommentText { get; private set; }
        public string? MaybeTargetUrl { get; private set; }
        public bool PromotionAfterPost { get; private set; }
        public int Count { get; private set; }
        public DelayPreset Delay { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public UserTaskStatus Status { get; set; }
        public int Progress { get; set; }
        public bool NotifyOnComplete { get; private set; }
        public bool NotifyOnError { get; private set; }
        public bool NotifyTelegram { get; private set; }
        public ParsingResult? Result { get; set; }
        public ICollection<TaskAccount> TaskAccounts { get; private set; } = new List<TaskAccount>();
        public ICollection<TaskLog> Logs { get; private set; } = new List<TaskLog>();

        private UserTask() { }

        public UserTask(string userId, TaskType type, string targetUrl, int count,
                        DelayPreset delay,
                        string? commentText = null, string? maybeTargetUrl = null,
                        bool promotionAfterPost = false,
                        bool notifyOnComplete = true, bool notifyOnError = true, bool notifyTelegram = false)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Type = type;
            TargetUrl = targetUrl;
            Count = count;
            Delay = delay;
            CommentText = commentText;
            MaybeTargetUrl = maybeTargetUrl;
            PromotionAfterPost = promotionAfterPost;
            NotifyOnComplete = notifyOnComplete;
            NotifyOnError = notifyOnError;
            NotifyTelegram = notifyTelegram;
            CreatedAt = DateTime.UtcNow;
            Status = UserTaskStatus.Pending;
        }
    }
}
