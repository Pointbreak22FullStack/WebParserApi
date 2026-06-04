using Application.Common.Interfaces;
using MediatR;

namespace Application.CQRS.Verification.Commands.ReceiveCode
{
    public class ReceiveCodeHandler : IRequestHandler<ReceiveCodeCommand>
    {
        private readonly ICodeStore _codeStore;
        public ReceiveCodeHandler(ICodeStore codeStore) => _codeStore = codeStore;

        public Task Handle(ReceiveCodeCommand request, CancellationToken ct)
        {
            // Нормализуем код: "123-456" → "123456"
            var code = request.Code.Replace("-", "").Replace(" ", "").Trim();

            // Нормализуем телефон: убираем лишние символы (нормализация внутри Store,
            // но передаём уже без лишних символов для читаемости логов)
            var phone = request.PhoneNumber.Trim();

            _codeStore.ProvideCodeByPhone(phone, code);
            return Task.CompletedTask;
        }
    }
}
