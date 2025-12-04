using System.ComponentModel.DataAnnotations;

namespace TeleICU.API.DTOs.AuthDtos
{
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string TempPassword { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; }
    }

    public class ResetPasswordOtp
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(6, MinimumLength = 4, ErrorMessage = "OTP must be 4-6 digits")]
        public string Otp { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; }
    }
}