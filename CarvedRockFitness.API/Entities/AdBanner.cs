using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Entities;

public class AdBanner
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string BusinessName { get; set; }

    [Required]
    public required string ImageUrl { get; set; }

    public string? LinkUrl { get; set; }

    public string? TagLine { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDemoData { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
