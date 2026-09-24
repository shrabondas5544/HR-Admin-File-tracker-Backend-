using System.ComponentModel.DataAnnotations;

namespace CabinetMap.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Designation { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = "Male"; // "Male" | "Female" | "Other"

    [MaxLength(20)]
    public string Role { get; set; } = "User"; // "Admin" | "User"

    [MaxLength(50)]
    public string? ResetCode { get; set; }

    public DateTime? ResetCodeExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
