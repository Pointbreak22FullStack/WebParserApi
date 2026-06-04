using MediatR;

namespace Application.Common.Notifications
{
    public enum ChangeType { Created, Updated, Deleted }

    public record EntityChangedNotification(
        string Group,
        ChangeType ChangeType,
        object? Payload) : INotification;
}
