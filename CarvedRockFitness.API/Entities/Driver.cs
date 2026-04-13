using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Entities;

public class Driver
{
    public int Id { get; set; }

    [Required] public required string FullName       { get; set; }
    [Required] public required string Phone          { get; set; }
    [Required] public required string Route          { get; set; }
    public string? Vehicle          { get; set; }
    public int     Capacity         { get; set; } = 7;
    public bool    PoliceClearance  { get; set; } = false;
    public string? LicenseNumber    { get; set; }
    public string  Status           { get; set; } = "Active"; // Active, Inactive, Suspended
    public bool    IsDemoData       { get; set; } = false;
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
}
