namespace Application.Common.DTOs
{
    public record TaskLogEntry(
        Guid   TaskId,
        string UserId,        // Identity-пользователь, запустивший задачу
        string AccountLogin,  // Парсер-аккаунт, выполняющий действие (логин аккаунта)
        double Progress,      // 0–100, общий прогресс задачи
        string Elapsed,       // "00:01:23"
        string Status,        // "info" | "success" | "error" | "waiting"
        string Message,
        DateTime Timestamp);
}
