using Application.CQRS.Parser.Commands.OpenUrl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ParserController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ParserController(IMediator mediator) => _mediator = mediator;

        [HttpPost("open")]
        public async Task<IActionResult> OpenBrowser([FromBody] OpenUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.Url)) return BadRequest("URL is required");
            await _mediator.Send(new OpenUrlCommand(request.Url));
            return Ok();
        }
    }

    public record OpenUrlRequest(string Url);
}
