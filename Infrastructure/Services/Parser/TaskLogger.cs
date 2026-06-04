using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Notifications;
using MediatR;

namespace Infrastructure.Services.Parser
{
    /// <summary>
    /// Один экземпляр на аккаунт внутри задачи.
    /// Прогресс рассчитывается как вклад этого аккаунта в общий пул:
    ///   overall = (completedAccounts + localPercent/100) / totalAccounts * 100
    /// </summary>
    internal sealed class TaskLogger : ITaskLogger
    {
        private readonly IPublisher _publisher;
        private readonly string     _group;
        private readonly Guid       _taskId;
        private readonly string     _userId;
        private readonly string     _accountLogin;
        private readonly DateTime   _startedAt;
        private readonly int        _totalAccounts;
        private readonly int        _accountIndex;

        private readonly int[] _completedRef;  // shared int[] { completedCount } между всеми логгерами задачи
        private double _localPercent;

        public TaskLogger(
            IPublisher publisher,
            Guid       taskId,
            string     userId,
            string     accountLogin,
            DateTime   startedAt,
            int        totalAccounts,
            int        accountIndex,
            int[]      completedRef)
        {
            _publisher     = publisher;
            _group         = $"user-logs:{userId}";
            _taskId        = taskId;
            _userId        = userId;
            _accountLogin  = accountLogin;
            _startedAt     = startedAt;
            _totalAccounts = totalAccounts;
            _accountIndex  = accountIndex;
            _completedRef  = completedRef;
        }

        public void SetProgress(double percent) => _localPercent = Math.Clamp(percent, 0, 100);

        public Task InfoAsync(string message)    => SendAsync(message, "info");
        public Task SuccessAsync(string message) => SendAsync(message, "success");
        public Task ErrorAsync(string message)   => SendAsync(message, "error");
        public Task WaitingAsync(string message) => SendAsync(message, "waiting");

        private Task SendAsync(string message, string status)
        {
            var completed = Volatile.Read(ref _completedRef[0]);
            var overall   = _totalAccounts == 0
                ? 100.0
                : Math.Min(100.0, (completed * 100.0 + _localPercent) / _totalAccounts);

            var entry = new TaskLogEntry(
                _taskId,
                _userId,
                _accountLogin,
                Math.Round(overall, 1),
                (DateTime.UtcNow - _startedAt).ToString(@"hh\:mm\:ss"),
                status,
                message,
                DateTime.UtcNow);

            return _publisher.Publish(new TaskLogNotification(_group, entry));
        }

        /// <summary>Вызвать после завершения аккаунта — обновляет общий счётчик.</summary>
        public void MarkCompleted() => Interlocked.Increment(ref _completedRef[0]);
    }
}
