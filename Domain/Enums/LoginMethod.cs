namespace Domain.Enums
{
    /// <summary>
    /// Через что аккаунт входит на площадку.
    /// Вход Яндекса и Mail.ru удалён (значения 0–2 больше не используются).
    /// </summary>
    public enum LoginMethod
    {
        VK        = 3,
        Google    = 4, // YouTube и Google-аккаунты (OAuth)
        TikTok    = 5, // TikTok Login Kit (OAuth)
        Instagram = 6, // Instagram Graph API через вход Facebook (OAuth)
    }
}
