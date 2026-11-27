using MediatR;

namespace Application.CQRS.Tasks.Commands.CancelTask
{
    /// <returns>true — отмена принята; false — задача уже завершена.</returns>
    public record CancelTaskCommand(Guid Id) : IRequest<bool>;
}
