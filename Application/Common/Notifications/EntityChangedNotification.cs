using MediatR;

namespace Application.Common.Notifications
{
    public enum ChangeType { Created, Updated, Deleted }

    public record EntityChangedNotification(
        string Group,
        ChangeType ChangeType,
        object? Payload,
        // Владелец сущности: если задан, уведомление уходит только ему и администраторам
        string? OwnerUserId = null) : INotification;
}
