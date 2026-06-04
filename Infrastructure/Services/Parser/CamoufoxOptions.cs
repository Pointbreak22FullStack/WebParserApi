namespace Infrastructure.Services.Parser
{
    public sealed class CamoufoxOptions
    {
        public const string Section = "Camoufox";

        /// <summary>HTTP-эндпоинт контейнера Camoufox, который отдаёт WebSocket URL браузера.</summary>
        public string HttpUrl { get; set; } = "http://localhost:3000";
    }
}
