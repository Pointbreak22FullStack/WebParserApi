using Application.CQRS.Proxy.Commands.CreateProxy;
using Application.CQRS.Proxy.Commands.DeleteProxy;
using Application.CQRS.Proxy.Commands.UpdateProxy;
using Application.CQRS.Proxy.DTOs;
using Application.CQRS.Proxy.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxiesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProxiesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProxyDto>>> GetAll()
            => Ok(await _mediator.Send(new GetProxiesQuery()));

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateProxyCommand command)
            => Ok(await _mediator.Send(command));

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, UpdateProxyCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID в пути и в теле запроса не совпадают");

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteProxyCommand(id));
            return NoContent();
        }
    }
}
