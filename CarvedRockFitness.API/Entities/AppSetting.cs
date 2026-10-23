using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Entities;

/// <summary>
/// Simple key/value store for admin-togglable runtime settings (e.g. demo-data visibility)
/// so they survive app restarts/redeploys instead of living only in process memory.
/// </summary>
public class AppSetting
{
    [Key]
    [MaxLength(100)]
    public required string Key { get; set; }

    [Required]
    public required string Value { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
