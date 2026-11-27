using Application.Common.Interfaces;
using Application.CQRS.Tasks.DTOs;
using MediatR;

namespace Application.CQRS.Tasks.Queries
{
    public class GetTasksHandler : IRequestHandler<GetTasksQuery, IEnumerable<UserTaskDto>>
    {
        private readonly ITaskRepository _repository;
        private readonly ICurrentUserService _currentUser;

        public GetTasksHandler(ITaskRepository repository, ICurrentUserService currentUser)
        {
            _repository = repository;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<UserTaskDto>> Handle(GetTasksQuery request, CancellationToken ct)
        {
            // Обычный пользователь видит только свои задачи, администратор — все
            var userId = _currentUser.IsAdmin ? null : _currentUser.UserId;
            var tasks = await _repository.GetAllAsync(userId, ct);
            return tasks.Select(UserTaskMapper.ToDto);
        }
    }
}
