using MediatR;

namespace Application.CQRS.Tasks.Commands.DeleteTask
{
    /// <returns>true — удалена; false — задача ещё выполняется (сначала её нужно отменить).</returns>
    public record DeleteTaskCommand(Guid Id) : IRequest<bool>;
}
