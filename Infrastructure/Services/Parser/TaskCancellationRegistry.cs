using System.Collections.Concurrent;
using Application.Common.Interfaces;

namespace Infrastructure.Services.Parser
{
    /// <summary>
    /// Токены отмены задач в памяти процесса. Задачи выполняет Hangfire-сервер этого же процесса,
    /// поэтому при одном экземпляре API этого достаточно.
    /// </summary>
    public sealed class TaskCancellationRegistry : ITaskCancellationRegistry
    {
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sources = new();

        public CancellationTokenSource Register(Guid taskId, CancellationToken linked)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(linked);
            _sources[taskId] = cts;
            return cts;
        }

        public bool Cancel(Guid taskId)
        {
            if (!_sources.TryGetValue(taskId, out var cts))
                return false;

            cts.Cancel();
            return true;
        }

        public void Unregister(Guid taskId)
        {
            if (_sources.TryRemove(taskId, out var cts))
                cts.Dispose();
        }
    }
}
