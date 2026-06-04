using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Infrastructure.Services.Verification
{
    public sealed class InMemoryCodeStore : ICodeStore
    {
        private readonly ILogger<InMemoryCodeStore> _logger;

        // accountId → очередь барьеров (несколько кодов в одном флоу)
        private readonly ConcurrentDictionary<int, Queue<TaskCompletionSource<string>>> _pending = new();

        // phone (нормализован) → accountId
        private readonly ConcurrentDictionary<string, int> _phoneIndex = new();

        // phone (нормализован) → (код, время получения)
        // TTL: коды старше EarlyBufferTtl автоматически игнорируются
        private readonly ConcurrentDictionary<string, (string Code, DateTime ReceivedAt)> _earlyBuffer = new();

        private static readonly TimeSpan EarlyBufferTtl = TimeSpan.FromMinutes(5);

        private readonly object _lock = new();

        public InMemoryCodeStore(ILogger<InMemoryCodeStore> logger)
        {
            _logger = logger;
        }

        public void RegisterPhone(int accountId, string phoneNumber)
        {
            var phone = Normalize(phoneNumber);
            lock (_lock)
            {
                _phoneIndex[phone] = accountId;

                // Сбрасываем earlyBuffer при старте новой задачи — коды, накопленные
                // до регистрации (ретраи форвардера с прошлых сессий), не нужны.
                // Свежий код придёт после того как браузер запросит SMS у Яндекса.
                if (_earlyBuffer.TryRemove(phone, out var dropped))
                {
                    var age = DateTime.UtcNow - dropped.ReceivedAt;
                    _logger.LogInformation(
                        "[CodeStore] RegisterPhone: earlyBuffer для phone='{Phone}' очищен (age={Age:mm\\:ss}, code='{Code}').",
                        phone, age, dropped.Code);
                }
            }
            _logger.LogInformation("[CodeStore] RegisterPhone: accountId={AccountId}, phone='{Phone}'", accountId, phone);
        }

        public Task<string> WaitForCodeAsync(int accountId, string phoneNumber, CancellationToken ct = default)
        {
            var phone = Normalize(phoneNumber);
            _logger.LogInformation(
                "[CodeStore] WaitForCodeAsync: accountId={AccountId}, raw='{Raw}', normalized='{Phone}'. Зарегистрированные телефоны: [{Phones}]",
                accountId, phoneNumber, phone,
                string.Join(", ", _phoneIndex.Keys));

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            ct.Register(() => tcs.TrySetCanceled(ct));

            lock (_lock)
            {
                _phoneIndex[phone] = accountId;

                // Если SMS уже пришла до нашего вызова — проверяем TTL и отдаём
                if (_earlyBuffer.TryRemove(phone, out var entry))
                {
                    var age = DateTime.UtcNow - entry.ReceivedAt;
                    if (age <= EarlyBufferTtl)
                    {
                        _logger.LogInformation(
                            "[CodeStore] earlyBuffer hit для phone='{Phone}', code='{Code}', age={Age:mm\\:ss}",
                            phone, entry.Code, age);
                        tcs.TrySetResult(entry.Code);
                        return tcs.Task;
                    }

                    _logger.LogWarning(
                        "[CodeStore] earlyBuffer ПРОСРОЧЕН для phone='{Phone}', age={Age:mm\\:ss} > TTL {Ttl:mm\\:ss}. Код отброшен.",
                        phone, age, EarlyBufferTtl);
                }

                _logger.LogInformation("[CodeStore] earlyBuffer miss для phone='{Phone}', TCS поставлен в очередь.", phone);
                var queue = _pending.GetOrAdd(accountId, _ => new Queue<TaskCompletionSource<string>>());
                queue.Enqueue(tcs);
            }

            return tcs.Task;
        }

        public void ProvideCodeByPhone(string phoneNumber, string code)
        {
            var phone = Normalize(phoneNumber);
            _logger.LogInformation(
                "[CodeStore] ProvideCodeByPhone: raw='{Raw}', normalized='{Phone}', code='{Code}'. Зарегистрированные телефоны: [{Phones}]",
                phoneNumber, phone, code,
                string.Join(", ", _phoneIndex.Keys));

            lock (_lock)
            {
                if (!_phoneIndex.TryGetValue(phone, out var accountId))
                {
                    // Телефон не найден в индексе.
                    // Fallback: если ровно один аккаунт ждёт кода — доставляем ему.
                    // Это покрывает кейс когда Яндекс шлёт SMS на другой номер
                    // (например, на номер привязанного устройства, а не на account.PhoneNumber).
                    // Именно так работает тестовый CodeServer — без привязки к номеру.
                    var waitingQueues = _pending
                        .Where(kv => kv.Value.Count > 0)
                        .ToList();

                    if (waitingQueues.Count == 1)
                    {
                        var (waitingId, waitingQueue) = waitingQueues[0];
                        _logger.LogWarning(
                            "[CodeStore] phone='{Phone}' не найден, но ровно один аккаунт ждёт кода (accountId={Id}). Доставляю (fallback). Ключи: [{Keys}]",
                            phone, waitingId, string.Join(", ", _phoneIndex.Keys));
                        waitingQueue.Dequeue().TrySetResult(code);
                        return;
                    }

                    // Несколько или ноль ждущих — нельзя определить кому, кладём в буфер.
                    if (_earlyBuffer.TryGetValue(phone, out var existing))
                    {
                        var age = DateTime.UtcNow - existing.ReceivedAt;
                        _logger.LogWarning(
                            "[CodeStore] МИСМАТЧ! phone='{Phone}' не найден. Обновляю earlyBuffer (предыдущий age={Age:mm\\:ss}). Ключи: [{Keys}]",
                            phone, age, string.Join(", ", _phoneIndex.Keys));
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[CodeStore] МИСМАТЧ! phone='{Phone}' не найден. Кладу в earlyBuffer. Ключи: [{Keys}]",
                            phone, string.Join(", ", _phoneIndex.Keys));
                    }

                    _earlyBuffer[phone] = (code, DateTime.UtcNow);
                    return;
                }

                if (!_pending.TryGetValue(accountId, out var queue) || queue.Count == 0)
                {
                    _logger.LogWarning("[CodeStore] Нет ожидающих TCS для accountId={AccountId}, кладу в earlyBuffer.", accountId);
                    _earlyBuffer[phone] = (code, DateTime.UtcNow);
                    return;
                }

                _logger.LogInformation("[CodeStore] Код '{Code}' доставлен в TCS для accountId={AccountId}.", code, accountId);
                queue.Dequeue().TrySetResult(code);
            }
        }

        public void Release(int accountId)
        {
            lock (_lock)
            {
                if (_pending.TryRemove(accountId, out var queue))
                {
                    while (queue.Count > 0)
                        queue.Dequeue().TrySetCanceled();
                }

                var phones = _phoneIndex.Where(kv => kv.Value == accountId).Select(kv => kv.Key).ToList();
                foreach (var phone in phones)
                {
                    _phoneIndex.TryRemove(phone, out _);
                    _earlyBuffer.TryRemove(phone, out _);
                }
            }
        }

        /// <summary>
        /// Нормализует номер телефона: оставляет только цифры.
        ///
        /// Фронтенд всегда объединяет код страны и номер в международный формат (+XXXXXXXXXXXXX),
        /// поэтому достаточно просто убрать всё кроме цифр — никаких правил по странам не нужно.
        ///
        ///   +373 68 123 456   →  37368123456
        ///   +7 (999) 123-4567 →  79991234567
        ///   +1-650-253-0000   →  16502530000
        /// </summary>
        private static string Normalize(string phone) =>
            string.IsNullOrWhiteSpace(phone)
                ? string.Empty
                : new string(phone.Where(char.IsDigit).ToArray());
    }
}
