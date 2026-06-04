using Domain.Enums;

namespace Domain.Entities
{
    public class Account
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string? Description { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public AccountStatus Status { get; set; }
        public int CompletedTasksCount { get; set; }
        public int? ProxyId { get; set; }
        public Proxy? Proxy { get; set; }
        public DateTime? LastUsedAtUtc { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CurrentHandlerUserId { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public LoginMethod LoginMethod { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? SessionInfo { get; set; } = string.Empty;
        public DateTime? SessionExpiresAtUtc { get; set; }
        public int RiskScore { get; set; }

        private Account() { }

        public Account(string login, string password, string? description,
                       string? firstName, string? lastName,
                       LoginMethod loginMethod, string phoneNumber,
                       int? proxyId = null)
        {
            Login = login;
            Password = password;
            Description = description;
            FirstName = firstName;
            LastName = lastName;
            LoginMethod = loginMethod;
            PhoneNumber = phoneNumber;
            ProxyId = proxyId;
            Status = AccountStatus.Active;
            CreatedAt = DateTime.UtcNow;
            CompletedTasksCount = 0;
            RiskScore = 0;
        }

        public void UpdateFromForm(string login, string password, string? description,
                                   string? firstName, string? lastName,
                                   LoginMethod loginMethod, string phoneNumber,
                                   int? proxyId = null)
        {
            // Если изменилось что-либо, влияющее на авторизацию — сбрасываем сессию,
            // чтобы следующий запуск выполнил полный вход заново.
            var sessionInvalidated =
                Login       != login       ||
                Password    != password    ||
                PhoneNumber != phoneNumber ||
                LoginMethod != loginMethod ||
                FirstName   != firstName   ||
                LastName    != lastName;

            if (sessionInvalidated)
            {
                SessionInfo         = null;
                SessionExpiresAtUtc = null;
            }

            Login       = login;
            Password    = password;
            Description = description;
            FirstName   = firstName;
            LastName    = lastName;
            LoginMethod = loginMethod;
            PhoneNumber = phoneNumber;
            ProxyId     = proxyId;
        }

        public void MarkAsBanned() => Status = AccountStatus.Banned;

        public void RequireCaptcha() => Status = AccountStatus.CaptchaRequired;

        public void SetCooldown(int minutes)
        {
            Status = AccountStatus.Cooldown;
            LockedUntilUtc = DateTime.UtcNow.AddMinutes(minutes);
        }

        public void IncrementTaskCount()
        {
            CompletedTasksCount++;
            LastUsedAtUtc = DateTime.UtcNow;
        }
    }
}
