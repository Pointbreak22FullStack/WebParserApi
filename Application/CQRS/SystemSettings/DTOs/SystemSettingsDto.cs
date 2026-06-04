namespace Application.CQRS.SystemSettings.DTOs
{
    public record SystemSettingsDto
    {
        public int DelayBetweenActionsMs { get; init; } = 800;
        public int MaxParallelTasks { get; init; } = 10;
        public bool RetryOnError { get; init; } = true;
        public bool HeadlessMode { get; init; } = true;
        public string FingerprintEngine { get; init; } = "Собственный";
        public bool RotateUserAgent { get; init; } = true;
        public bool SimulateHumanBehavior { get; init; } = true;
        public string GenerationMode { get; init; } = "LLM";
        public int CheckIntervalMinutes { get; init; } = 30;
        public bool AutoLikeEnabled { get; init; } = true;
        public bool ReportCompetitors { get; init; } = false;
        public string? TelegramBotToken { get; init; }
        public string? TelegramChatId { get; init; }
    }
}
