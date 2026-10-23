using System.ComponentModel.DataAnnotations;

namespace CarvedRockFitness.API.Entities;

public class AcademyRegistration
{
    public int Id { get; set; }

    [Required] public required string PlayerName   { get; set; }
    public int    Age           { get; set; }
    [Required] public required string Sport         { get; set; }

    [Required] public required string ParentName    { get; set; }
    [Required] public required string ParentEmail   { get; set; }
    [Required] public required string ParentPhone   { get; set; }

    public bool   NeedsTransport { get; set; }
    public string? PickupAddress  { get; set; }
    public string? Notes          { get; set; }

    public string  Status        { get; set; } = "Pending"; // Pending, Confirmed, Active, Inactive
    public string? AssignedRoute  { get; set; }

    public bool    IsDemoData    { get; set; } = false;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // POPIA: records that the registering parent/guardian consented to the processing of their
    // and their child's personal information at the time of registration.
    public DateTime? ConsentGivenAt { get; set; }
}
