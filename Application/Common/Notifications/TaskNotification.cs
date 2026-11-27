using MediatR;

namespace Application.Common.Notifications
{
    /// <summary>Уведомление о завершении задачи: тост в интерфейсе и/или сообщение в Telegram.</summary>
    public record TaskNotification(
        string UserId,
        Guid TaskId,
        string Level,   // success | error | info
        string Message,
        bool ShowToast,
        bool SendToTelegram) : INotification;
}
