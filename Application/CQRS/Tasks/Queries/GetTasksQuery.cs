using Application.CQRS.Tasks.DTOs;
using MediatR;

namespace Application.CQRS.Tasks.Queries
{
    public record GetTasksQuery : IRequest<IEnumerable<UserTaskDto>>;
}
