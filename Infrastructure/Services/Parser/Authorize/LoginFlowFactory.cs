using Domain.Enums;

namespace Infrastructure.Services.Parser.Authorize
{
    public class LoginFlowFactory
    {
        private readonly IEnumerable<ILoginFlow> _flows;

        public LoginFlowFactory(IEnumerable<ILoginFlow> flows) => _flows = flows;

        public ILoginFlow GetFor(LoginMethod method)
            => _flows.FirstOrDefault(f => f.Method == method)
               ?? throw new InvalidOperationException($"No login flow registered for LoginMethod.{method}");
    }
}
