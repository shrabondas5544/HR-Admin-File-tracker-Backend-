using System.ComponentModel.DataAnnotations;

namespace CabinetMap.Api.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation is required.")]
    public string Designation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender selection is required.")]
    public string Gender { get; set; } = "Male"; // "Male" | "Female" | "Other"
}

public class LoginDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Current Password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New Password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset Code is required.")]
    public string ResetCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "New Password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new UserDto();
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
