using Domain.Enums;

namespace Domain.Entities
{
    public class Proxy
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public ProxyType Type { get; set; }
        public bool IsActive { get; set; }
        public int? LatencyMs { get; set; }
        public bool HasError { get; set; }
        public bool IsWarming { get; set; }
        public bool IsFree { get; set; }
        public ICollection<Account> Accounts { get; set; } = new List<Account>();

        private Proxy() { }

        public static Proxy Create(string ipAddress, int port, string login, string password, ProxyType type)
        {
            return new Proxy
            {
                IpAddress = ipAddress,
                Port = port,
                Login = login,
                Password = password,
                Type = type,
                IsActive = true,
                IsFree = true,
                HasError = false,
                IsWarming = false
            };
        }

        public void Update(string ipAddress, int port, string login, string password, ProxyType type, bool isActive)
        {
            IpAddress = ipAddress;
            Port      = port;
            Login     = login;
            Password  = password;
            Type      = type;
            IsActive  = isActive;
            HasError  = false;  // сброс ошибки при любом редактировании прокси
        }

        public void UpdateLatency(int latency)
        {
            LatencyMs = latency;
            IsActive = true;
        }

        public void SetError()
        {
            HasError  = true;
            IsActive  = false;
            LatencyMs = null;
        }
    }
}
