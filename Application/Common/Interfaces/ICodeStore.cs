namespace Application.Common.Interfaces
{
    public interface ICodeStore
    {
        /// <summary>
        /// Заранее привязывает номер телефона к аккаунту, чтобы ProvideCodeByPhone
        /// мог доставить код даже до вызова WaitForCodeAsync.
        /// Должен вызываться перед запуском браузерного флоу.
        /// </summary>
        void RegisterPhone(int accountId, string phoneNumber);

        /// <summary>
        /// Регистрирует ожидание кода для аккаунта. Внешний клиент идентифицирует аккаунт
        /// по номеру телефона и вызывает ProvideCodeByPhone — accountId при этом не нужен.
        /// </summary>
        Task<string> WaitForCodeAsync(int accountId, string phoneNumber, CancellationToken ct = default);

        /// <summary>Доставляет код по номеру телефона (вызывается из POST /api/code).</summary>
        void ProvideCodeByPhone(string phoneNumber, string code);

        /// <summary>Освобождает все слоты аккаунта после завершения работы.</summary>
        void Release(int accountId);
    }
}
