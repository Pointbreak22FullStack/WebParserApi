using Application.CQRS.SystemSettings.Commands;
using Application.CQRS.SystemSettings.DTOs;
using Application.CQRS.SystemSettings.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SettingsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<ActionResult<SystemSettingsDto>> Get()
        {
            return Ok(await _mediator.Send(new GetSettingsQuery()));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] SystemSettingsDto dto)
        {
            await _mediator.Send(new UpdateSettingsCommand(dto));
            return NoContent();
        }
    }
}
