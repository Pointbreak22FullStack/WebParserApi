using Application.CQRS.Verification.Commands.ReceiveCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class CodeController : ControllerBase
    {
        public const string SecretHeader = "X-Webhook-Secret";

        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;

        public CodeController(IMediator mediator, IConfiguration configuration)
        {
            _mediator = mediator;
            _configuration = configuration;
        }

        /// <summary>
        /// POST /api/code — доставить SMS/push-код по номеру телефона получателя.
        /// Body: { "phone": "+79991234567", "code": "123456" }
        /// Вызывается SMS-клиентом или агрегатором push-уведомлений; accountId не нужен.
        /// Требует заголовок X-Webhook-Secret со значением CodeWebhook:Secret.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] ReceiveCodeRequest request)
        {
            var expected = _configuration["CodeWebhook:Secret"];
            if (string.IsNullOrWhiteSpace(expected))
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Приём кодов отключён: не задан CodeWebhook:Secret.");

            var provided = Request.Headers[SecretHeader].ToString();
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
                return Unauthorized();

            await _mediator.Send(new ReceiveCodeCommand(request.Phone, request.Code));
            return Ok();
        }
    }

    public record ReceiveCodeRequest(string Phone, string Code);
}
