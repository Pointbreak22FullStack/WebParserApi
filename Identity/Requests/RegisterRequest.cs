using System.ComponentModel.DataAnnotations;

namespace Identity.Requests
{
    public class RegisterRequest
    {
        [Required] // Добавляем имя
        public string Name { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
