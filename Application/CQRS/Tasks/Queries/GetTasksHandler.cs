using Application.Common.Interfaces;
using Application.CQRS.Tasks.DTOs;
using MediatR;

namespace Application.CQRS.Tasks.Queries
{
    public class GetTasksHandler : IRequestHandler<GetTasksQuery, IEnumerable<UserTaskDto>>
    {
        private readonly ITaskRepository _repository;

        public GetTasksHandler(ITaskRepository repository) => _repository = repository;

        public async Task<IEnumerable<UserTaskDto>> Handle(GetTasksQuery request, CancellationToken ct)
        {
            var tasks = await _repository.GetAllAsync(ct);
            return tasks.Select(UserTaskMapper.ToDto);
        }
    }
}
