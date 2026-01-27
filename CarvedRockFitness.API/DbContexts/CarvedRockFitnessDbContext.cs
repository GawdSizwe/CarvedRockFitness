using CarvedRockFitness.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.DbContexts;

public class CarvedRockFitnessDbContext(DbContextOptions<CarvedRockFitnessDbContext> options) : DbContext(options)
{
    public DbSet<Routine> Routines { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Trainees)
            .WithOne(u => u.Trainer)
            .HasForeignKey(u => u.TrainerId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Routines)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        // Seed data
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "John Doe", Email = "john.doe@carvedrockfitness.com", Role = "Admin" },
            new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@carvedrockfitness.com", Role = "Trainer" },
            new User { Id = 3, Name = "Mike Johnson", Email = "mike.johnson@carvedrockfitness.com", Role = "Trainer" },
            new User { Id = 4, Name = "Emily Davis", Email = "emily.davis@carvedrockfitness.com", Role = "Trainee", TrainerId = 2 },
            new User { Id = 5, Name = "Chris Brown", Email = "chris.brown@carvedrockfitness.com", Role = "Trainee", TrainerId = 2 },
            new User { Id = 6, Name = "Patricia Taylor", Email = "patricia.taylor@carvedrockfitness.com", Role = "Trainee", TrainerId = 3 },
            new User { Id = 7, Name = "Robert Wilson", Email = "robert.wilson@carvedrockfitness.com", Role = "Trainee", TrainerId = 3 },
            new User { Id = 8, Name = "Linda Martinez", Email = "linda.martinez@carvedrockfitness.com", Role = "Trainee", TrainerId = 3 },
            new User { Id = 9, Name = "David Anderson", Email = "david.anderson@carvedrockfitness.com", Role = "Trainee", TrainerId = 2 },
            new User { Id = 10, Name = "Sarah Thomas", Email = "sarah.thomas@carvedrockfitness.com", Role = "Trainee", TrainerId = 2 }
        );

        modelBuilder.Entity<Routine>().HasData(
            new Routine { Id = 1, Name = "Morning Yoga", Description = "A relaxing morning yoga routine", UserId = 4 },
            new Routine { Id = 2, Name = "Cardio Blast", Description = "High-intensity cardio workout", UserId = 5 },
            new Routine { Id = 3, Name = "Strength Training", Description = "Full-body strength training", UserId = 6 },
            new Routine { Id = 4, Name = "HIIT Workout", Description = "High-Intensity Interval Training", UserId = 7 },
            new Routine { Id = 5, Name = "Pilates", Description = "Core strengthening Pilates routine", UserId = 8 },
            new Routine { Id = 6, Name = "Evening Stretch", Description = "Gentle evening stretching routine", UserId = 9 },
            new Routine { Id = 7, Name = "Upper Body Strength", Description = "Upper body strength training", UserId = 10 },
            new Routine { Id = 8, Name = "Lower Body Strength", Description = "Lower body strength training", UserId = 4 },
            new Routine { Id = 9, Name = "Full Body Workout", Description = "Comprehensive full-body workout", UserId = 5 },
            new Routine { Id = 10, Name = "Core Workout", Description = "Intense core workout", UserId = 6 },
            new Routine { Id = 11, Name = "Flexibility Training", Description = "Flexibility and mobility exercises", UserId = 7 },
            new Routine { Id = 12, Name = "Balance Training", Description = "Balance and stability exercises", UserId = 8 },
            new Routine { Id = 13, Name = "Endurance Training", Description = "Endurance and stamina building", UserId = 9 },
            new Routine { Id = 14, Name = "Speed Training", Description = "Speed and agility drills", UserId = 10 },
            new Routine { Id = 15, Name = "Power Training", Description = "Power and explosiveness training", UserId = 4 },
            new Routine { Id = 16, Name = "Recovery Workout", Description = "Active recovery exercises", UserId = 5 },
            new Routine { Id = 17, Name = "Circuit Training", Description = "Circuit-based workout routine", UserId = 6 },
            new Routine { Id = 18, Name = "Bodyweight Workout", Description = "Bodyweight exercises", UserId = 7 },
            new Routine { Id = 19, Name = "Kettlebell Workout", Description = "Kettlebell exercises", UserId = 8 },
            new Routine { Id = 20, Name = "Dumbbell Workout", Description = "Dumbbell exercises", UserId = 9 },
            new Routine { Id = 21, Name = "Barbell Workout", Description = "Barbell exercises", UserId = 10 },
            new Routine { Id = 22, Name = "Resistance Band Workout", Description = "Resistance band exercises", UserId = 4 },
            new Routine { Id = 23, Name = "TRX Workout", Description = "TRX suspension training", UserId = 5 },
            new Routine { Id = 24, Name = "Plyometrics", Description = "Plyometric exercises", UserId = 6 },
            new Routine { Id = 25, Name = "Boxing Workout", Description = "Boxing and kickboxing drills", UserId = 7 },
            new Routine { Id = 26, Name = "Martial Arts Training", Description = "Martial arts techniques", UserId = 8 },
            new Routine { Id = 27, Name = "Dance Workout", Description = "Dance-based fitness routine", UserId = 9 },
            new Routine { Id = 28, Name = "Swimming Workout", Description = "Swimming drills and exercises", UserId = 10 },
            new Routine { Id = 29, Name = "Cycling Workout", Description = "Cycling drills and exercises", UserId = 4 },
            new Routine { Id = 30, Name = "Rowing Workout", Description = "Rowing drills and exercises", UserId = 5 }
        );

    }
}
