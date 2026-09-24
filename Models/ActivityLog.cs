using System.ComponentModel.DataAnnotations;

namespace CabinetMap.Api.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    [MaxLength(100)]
    public string UserName { get; set; } = "Anonymous System";

    [MaxLength(150)]
    public string UserEmail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UserDesignation { get; set; } = string.Empty;

    [MaxLength(20)]
    public string UserGender { get; set; } = "Male";

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = "UNKNOWN"; // "CREATE", "MOVE", "DELETE", "RESTORE", "TRANSFER", "LOGIN", "REGISTER", "PASSWORD_CHANGE"

    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // "File", "Folder", "Magazine", "Shelf", "Cabinet", "User"

    public int? EntityId { get; set; }

    [MaxLength(200)]
    public string EntityTitle { get; set; } = string.Empty;

    [Required]
    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
