using Domain.Enums;

namespace Domain.Entities
{
    public class TaskLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParserTaskId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public string? AccountName { get; set; }
        public LogLevel Level { get; set; }
        public string? StatusText { get; set; }
        public string? QuestionNumber { get; set; }
    }
}
