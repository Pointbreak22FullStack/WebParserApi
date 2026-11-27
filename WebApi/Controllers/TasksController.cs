using Application.CQRS.Tasks.Commands;
using Application.CQRS.Tasks.Commands.CancelTask;
using Application.CQRS.Tasks.Commands.DeleteTask;
using Application.CQRS.Tasks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _mediator.Send(new GetTasksQuery()));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>
        /// Отменить задачу: ожидающая не запустится, выполняющаяся остановится после текущего шага.
        /// 202 — отмена принята, 404 — задачи нет, 409 — задача уже завершена.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
            => await _mediator.Send(new CancelTaskCommand(id))
                ? Accepted()
                : Conflict("Задача уже завершена.");

        /// <summary>Удалить завершённую задачу. 409 — задача ещё выполняется.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _mediator.Send(new DeleteTaskCommand(id))
                ? NoContent()
                : Conflict("Задача выполняется — сначала отмените её.");
    }
}
