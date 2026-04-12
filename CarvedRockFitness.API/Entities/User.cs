using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CarvedRockFitness.API.Entities;

[Table("Users")]
public class User : IdentityUser<int>
{
    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Role { get; set; } // "Trainer", "Trainee", "Admin", "FreeUser", "PaidUser"

    public bool IsPaidUser { get; set; }

    public ICollection<Routine> Routines { get; set; } = [];

    [InverseProperty("Trainer")]
    public ICollection<User> Trainees { get; set; } = [];

    [ForeignKey("TrainerId")]
    public int? TrainerId { get; set; }
    public User? Trainer { get; set; }  
}
