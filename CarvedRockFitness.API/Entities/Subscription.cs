using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarvedRockFitness.API.Entities;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public string? PaymentReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; }
}
