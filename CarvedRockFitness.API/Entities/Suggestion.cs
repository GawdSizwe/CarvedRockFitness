using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Entities;

public class Suggestion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public required string Description { get; set; }

    [Required]
    public required string Category { get; set; }

    [Required]
    public required string Location { get; set; }

    public string? ImagePath { get; set; }

    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EventDate { get; set; }

    public bool IsPinned    { get; set; } = false;
    public bool IsHidden    { get; set; } = false;
    public int  Priority    { get; set; } = 0;
    public bool IsDemoData  { get; set; } = false;

    public string? CreatedByEmail { get; set; }
    public string? CreatedByName  { get; set; }

    public ContactDetail? ContactDetail { get; set; }
}
