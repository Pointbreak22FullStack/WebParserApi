using Application.CQRS.Accounts.Commands.CreateAccount;
using Application.CQRS.Accounts.Commands.DeleteAccount;
using Application.CQRS.Accounts.Commands.UpdateAccount;
using Application.CQRS.Accounts.DTOs;
using Application.CQRS.Accounts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AccountsController(IMediator mediator) => _mediator = mediator;
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAll()
        {
            return Ok(await _mediator.Send(new GetAccountsQuery()));
        }
        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateAccountCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, UpdateAccountCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteAccountCommand(id));
            return NoContent();
        }
    }
}
