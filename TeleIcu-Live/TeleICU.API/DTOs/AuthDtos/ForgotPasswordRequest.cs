using System.ComponentModel.DataAnnotations;

namespace TeleICU.API.DTOs.AuthDtos
{
    public class ForgotPasswordEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
