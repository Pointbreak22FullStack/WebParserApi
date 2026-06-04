using Domain.Enums;

namespace Infrastructure.Services.Parser.Authorize
{
    /// <summary>
    /// Случайная задержка между действиями парсера на основе <see cref="DelayPreset"/>.
    ///
    /// Пример: <c>DelayPreset.Fast</c> → диапазон 1–3 с → каждый вызов <see cref="WaitAsync"/>
    /// ждёт случайное число миллисекунд из этого диапазона.
    /// </summary>
    public readonly struct ActionDelay
    {
        private readonly int _minMs;
        private readonly int _maxMs;

        public ActionDelay(DelayPreset preset)
        {
            var (from, to) = preset.ToSeconds();
            _minMs = from * 1000;
            _maxMs = to   * 1000;
        }

        /// <summary>
        /// Ждёт случайное время в диапазоне пресета.
        /// </summary>
        public Task WaitAsync(CancellationToken ct = default)
        {
            var ms = Random.Shared.Next(_minMs, _maxMs + 1);
            return Task.Delay(ms, ct);
        }
    }
}
