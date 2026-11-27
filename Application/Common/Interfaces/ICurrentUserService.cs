namespace Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }

        /// <summary>Пользователь с ролью Admin видит данные всех пользователей.</summary>
        bool IsAdmin { get; }
    }
}
