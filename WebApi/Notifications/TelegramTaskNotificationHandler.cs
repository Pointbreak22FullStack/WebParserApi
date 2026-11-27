using Application.Common.Notifications;
using Domain.Interfaces;
using MediatR;

namespace WebApi.Notifications
{
    /// <summary>
    /// Отправляет уведомление о завершении задачи в Telegram (бот и чат задаются в настройках системы).
    /// Ошибки отправки только логируются: уведомление не должно ломать выполнение задачи.
    /// </summary>
    public class TelegramTaskNotificationHandler : INotificationHandler<TaskNotification>
    {
        public const string HttpClientName = "telegram";

        private readonly ISystemSettingsRepository _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TelegramTaskNotificationHandler> _logger;

        public TelegramTaskNotificationHandler(
            ISystemSettingsRepository settings,
            IHttpClientFactory httpClientFactory,
            ILogger<TelegramTaskNotificationHandler> logger)
        {
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task Handle(TaskNotification notification, CancellationToken ct)
        {
            if (!notification.SendToTelegram)
                return;

            var settings = await _settings.GetAsync(ct);
            if (string.IsNullOrWhiteSpace(settings?.TelegramBotToken) || string.IsNullOrWhiteSpace(settings.TelegramChatId))
            {
                _logger.LogWarning("[Telegram] Уведомление по задаче {TaskId} пропущено: не заданы токен бота или chat id.",
                    notification.TaskId);
                return;
            }

            var icon = notification.Level switch { "success" => "✅", "error" => "❌", _ => "ℹ️" };
            var text = $"{icon} WebParser: {notification.Message}\nЗадача {notification.TaskId}";

            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);
                using var response = await client.PostAsJsonAsync(
                    $"bot{settings.TelegramBotToken}/sendMessage",
                    new { chat_id = settings.TelegramChatId, text },
                    ct);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("[Telegram] Не удалось отправить уведомление: HTTP {Status}.", (int)response.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "[Telegram] Не удалось отправить уведомление по задаче {TaskId}.", notification.TaskId);
            }
        }
    }
}
