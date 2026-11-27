namespace Application.Common.Interfaces
{
    /// <summary>
    /// Хранит токены отмены выполняющихся задач, чтобы их можно было остановить из интерфейса.
    /// </summary>
    public interface ITaskCancellationRegistry
    {
        /// <summary>Регистрирует задачу и возвращает источник токена, связанный с <paramref name="linked"/>.</summary>
        CancellationTokenSource Register(Guid taskId, CancellationToken linked);

        /// <summary>Запрашивает отмену. false — задача сейчас не выполняется.</summary>
        bool Cancel(Guid taskId);

        void Unregister(Guid taskId);
    }
}
