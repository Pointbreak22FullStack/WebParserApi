using System.Text.Json;

namespace Infrastructure.Services.Parser
{
    public static class SessionHelper
    {
        /// <summary>
        /// Парсит StorageState JSON Playwright и возвращает дату истечения
        /// как минимальный expires среди всех куки с конечным сроком действия.
        /// </summary>
        public static DateTime? ExtractExpiresAt(string? storageStateJson)
        {
            if (string.IsNullOrEmpty(storageStateJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(storageStateJson);
                if (!doc.RootElement.TryGetProperty("cookies", out var cookies))
                    return null;

                double? minExpires = null;

                foreach (var cookie in cookies.EnumerateArray())
                {
                    if (!cookie.TryGetProperty("expires", out var prop))
                        continue;

                    var ts = prop.GetDouble();
                    if (ts <= 0) continue; // -1 = сессионная кука, без срока

                    if (minExpires == null || ts < minExpires)
                        minExpires = ts;
                }

                if (minExpires == null)
                    return null;

                return DateTimeOffset.FromUnixTimeSeconds((long)minExpires.Value).UtcDateTime;
            }
            catch
            {
                return null;
            }
        }
    }
}
