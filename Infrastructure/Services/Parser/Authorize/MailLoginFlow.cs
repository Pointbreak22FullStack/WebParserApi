using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;

namespace Infrastructure.Services.Parser.Authorize
{
    public class MailLoginFlow : ILoginFlow
    {
        public LoginMethod Method => LoginMethod.MailRu;

        public Task LoginAsync(
            IBrowserContext    context,
            Account            account,
            string             targetUrl,
            Func<Task<string>> getCodeAsync,
            ActionDelay        delay,
            ITaskLogger        logger,
            CancellationToken  ct)
        {
            // TODO: реализовать вход через Mail.ru
            throw new NotImplementedException("Mail login flow is not implemented yet.");
        }
    }
}
