using Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.AI
{
    public sealed class ClaudeAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ClaudeOptions _options;

        public ClaudeAiService(HttpClient httpClient, IOptions<ClaudeOptions> options)
        {
            _httpClient = httpClient;
            _options    = options.Value;
        }

        public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
        {
            var body = BuildBody(new object[]
            {
                new { type = "text", text = prompt }
            });

            return await SendAsync(body, ct);
        }

        public async Task<string> CompleteWithImageAsync(byte[] imageBytes, string prompt, CancellationToken ct = default)
        {
            var body = BuildBody(new object[]
            {
                new
                {
                    type   = "image",
                    source = new
                    {
                        type       = "base64",
                        media_type = DetectMimeType(imageBytes),
                        data       = Convert.ToBase64String(imageBytes)
                    }
                },
                new { type = "text", text = prompt }
            });

            return await SendAsync(body, ct);
        }

        private object BuildBody(object[] content) => new
        {
            model      = _options.Model,
            max_tokens = _options.MaxTokens,
            messages   = new[]
            {
                new { role = "user", content }
            }
        };

        private async Task<string> SendAsync(object body, CancellationToken ct)
        {
            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/messages", content, ct);
            var raw      = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Claude API {(int)response.StatusCode}: {raw}");

            using var doc    = JsonDocument.Parse(raw);
            var contentArray = doc.RootElement.GetProperty("content");

            return contentArray.GetArrayLength() > 0
                ? contentArray[0].GetProperty("text").GetString()?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static string DetectMimeType(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes.Length >= 4 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF") return "image/webp";
            return "image/jpeg";
        }
    }
}
