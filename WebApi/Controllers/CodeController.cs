using Application.CQRS.Verification.Commands.ReceiveCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class CodeController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CodeController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// POST /api/code — доставить SMS/push-код по номеру телефона получателя.
        /// Body: { "phone": "+79991234567", "code": "123456" }
        /// Вызывается SMS-клиентом или агрегатором push-уведомлений; accountId не нужен.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] ReceiveCodeRequest request)
        {
            await _mediator.Send(new ReceiveCodeCommand(request.Phone, request.Code));
            return Ok();
        }
    }

    public record ReceiveCodeRequest(string Phone, string Code);
}
