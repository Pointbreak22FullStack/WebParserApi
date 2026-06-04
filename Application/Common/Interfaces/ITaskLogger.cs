namespace Application.Common.Interfaces
{
    public interface ITaskLogger
    {
        Task InfoAsync(string message);
        Task SuccessAsync(string message);
        Task ErrorAsync(string message);
        Task WaitingAsync(string message);

        /// <summary>Обновить прогресс (0–100). Отправляется вместе со следующим сообщением.</summary>
        void SetProgress(double percent);
    }
}
