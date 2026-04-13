using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Entities;

public class DriverApplication
{
    public int Id { get; set; }

    [Required] public required string FullName      { get; set; }
    [Required] public required string Phone         { get; set; }
    [Required] public required string Email         { get; set; }
    [Required] public required string PreferredRoute { get; set; }
    public string? Vehicle         { get; set; }
    public int     Capacity        { get; set; } = 7;
    public bool    HasPoliceClearance { get; set; } = false;
    public string? LicenseNumber   { get; set; }
    public string? Notes           { get; set; }

    public string  Status          { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
}
