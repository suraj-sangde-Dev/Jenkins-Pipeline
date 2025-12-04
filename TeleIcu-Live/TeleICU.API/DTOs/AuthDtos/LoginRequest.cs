using System.ComponentModel.DataAnnotations;

namespace TeleICU.API.DTOs.AuthDtos
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public int? StateId { get; set; }
        public string? StateName { get; set; }
        public string? StateCode { get; set; }
        public int? CoeId { set; get; }
        public string? CoeName { get; set; }
        public int? SpokeId { set; get; }
        public string? SpokeHospitalName { get; set; }
        public string ProfilePic { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; }
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}