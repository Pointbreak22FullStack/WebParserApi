using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Authorize
{
    public interface ILoginFlow
    {
        LoginMethod Method { get; }

        Task LoginAsync(
            IBrowserContext    context,
            Account            account,
            string             targetUrl,
            Func<Task<string>> getCodeAsync,
            ActionDelay        delay,
            ITaskLogger        logger,
            CancellationToken  ct);
    }
}
