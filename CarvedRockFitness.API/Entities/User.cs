using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarvedRockFitness.API.Entities;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Email { get; set; }

    [Required]
    public required string Role { get; set; } // "Trainer", "Trainee", "Admin", ...

    public ICollection<Routine> Routines { get; set; } = [];

    [InverseProperty("Trainer")]
    public ICollection<User> Trainees { get; set; } = [];

    [ForeignKey("TrainerId")]
    public int? TrainerId { get; set; }
    public User? Trainer { get; set; }  
}
