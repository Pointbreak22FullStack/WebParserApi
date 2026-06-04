using Application.Common.DTOs;
using MediatR;

namespace Application.Common.Notifications
{
    public record TaskLogNotification(string Group, TaskLogEntry Entry) : INotification;
}
