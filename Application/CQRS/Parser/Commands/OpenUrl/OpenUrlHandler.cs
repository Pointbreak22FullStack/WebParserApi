using Application.Common.Interfaces;
using MediatR;

namespace Application.CQRS.Parser.Commands.OpenUrl
{
    public class OpenUrlHandler : IRequestHandler<OpenUrlCommand>
    {
        private readonly IBrowserService _browserService;
        public OpenUrlHandler(IBrowserService browserService) => _browserService = browserService;

        public async Task Handle(OpenUrlCommand request, CancellationToken ct)
            => await _browserService.OpenPageAsync(request.Url, ct);
    }
}
