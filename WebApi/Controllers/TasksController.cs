using Application.CQRS.Tasks.Commands;
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
    }
}
