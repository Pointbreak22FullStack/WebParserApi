using Domain.Enums;

namespace Domain.Entities
{
    public class TaskAccount
    {
        public Guid Id { get; private set; }
        public Guid UserTaskId { get; private set; }
        public UserTask UserTask { get; private set; } = null!;
        public int AccountId { get; private set; }
        public Account Account { get; private set; } = null!;
        public TaskAccountStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? DurationSeconds { get; set; }

        private TaskAccount() { }

        public TaskAccount(Guid userTaskId, int accountId)
        {
            Id = Guid.NewGuid();
            UserTaskId = userTaskId;
            AccountId = accountId;
            Status = TaskAccountStatus.InQueue;
        }
    }
}
