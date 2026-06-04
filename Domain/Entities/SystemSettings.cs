namespace Domain.Entities
{
    public class SystemSettings
    {
        public int Id { get; set; }
        public int DelayBetweenActionsMs { get; set; }
        public int MaxParallelTasks { get; set; }
        public bool RetryOnError { get; set; }
        public bool HeadlessMode { get; set; }
        public string FingerprintEngine { get; set; } = "Default";
        public bool RotateUserAgent { get; set; }
        public bool SimulateHumanBehavior { get; set; }
        public string GenerationMode { get; set; } = "LLM";
        public int CheckIntervalMinutes { get; set; }
        public bool AutoLikeEnabled { get; set; }
        public bool ReportCompetitors { get; set; }
        public string? TelegramBotToken { get; set; }
        public string? TelegramChatId { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
