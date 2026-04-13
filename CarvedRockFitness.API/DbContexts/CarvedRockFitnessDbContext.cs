using CarvedRockFitness.API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.DbContexts;

public class CarvedRockFitnessDbContext(DbContextOptions<CarvedRockFitnessDbContext> options) 
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<Routine> Routines { get; set; }
    public override DbSet<User> Users { get; set; }
    public DbSet<Suggestion> Suggestions { get; set; }
    public DbSet<ContactDetail> ContactDetails { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<AdBanner> AdBanners { get; set; }
    public DbSet<AcademyRegistration> AcademyRegistrations { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<DriverApplication> DriverApplications { get; set; }
    public DbSet<DriverRating> DriverRatings { get; set; }
    public DbSet<AcademyReview> AcademyReviews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            new User { Id = 1, Name = "John Doe", UserName = "john.doe@carvedrockfitness.com", NormalizedUserName = "JOHN.DOE@CARVEDROCKFITNESS.COM", Email = "john.doe@carvedrockfitness.com", NormalizedEmail="JOHN.DOE@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-1", ConcurrencyStamp = "static-concurrency-1", Role = "Admin", IsPaidUser = true },
            new User { Id = 2, Name = "Jane Smith", UserName = "jane.smith@carvedrockfitness.com", NormalizedUserName = "JANE.SMITH@CARVEDROCKFITNESS.COM", Email = "jane.smith@carvedrockfitness.com", NormalizedEmail="JANE.SMITH@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-2", ConcurrencyStamp = "static-concurrency-2", Role = "Trainer", IsPaidUser = true },
            new User { Id = 3, Name = "Mike Johnson", UserName = "mike.johnson@carvedrockfitness.com", NormalizedUserName = "MIKE.JOHNSON@CARVEDROCKFITNESS.COM", Email = "mike.johnson@carvedrockfitness.com", NormalizedEmail="MIKE.JOHNSON@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-3", ConcurrencyStamp = "static-concurrency-3", Role = "Trainer", IsPaidUser = true },
            new User { Id = 4, Name = "Emily Davis", UserName = "emily.davis@carvedrockfitness.com", NormalizedUserName = "EMILY.DAVIS@CARVEDROCKFITNESS.COM", Email = "emily.davis@carvedrockfitness.com", NormalizedEmail="EMILY.DAVIS@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-4", ConcurrencyStamp = "static-concurrency-4", Role = "Trainee", TrainerId = 2 },
            new User { Id = 5, Name = "Chris Brown", UserName = "chris.brown@carvedrockfitness.com", NormalizedUserName = "CHRIS.BROWN@CARVEDROCKFITNESS.COM", Email = "chris.brown@carvedrockfitness.com", NormalizedEmail="CHRIS.BROWN@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-5", ConcurrencyStamp = "static-concurrency-5", Role = "Trainee", TrainerId = 2 },
            new User { Id = 6, Name = "Patricia Taylor", UserName = "patricia.taylor@carvedrockfitness.com", NormalizedUserName = "PATRICIA.TAYLOR@CARVEDROCKFITNESS.COM", Email = "patricia.taylor@carvedrockfitness.com", NormalizedEmail="PATRICIA.TAYLOR@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-6", ConcurrencyStamp = "static-concurrency-6", Role = "Trainee", TrainerId = 3 },
            new User { Id = 7, Name = "Robert Wilson", UserName = "robert.wilson@carvedrockfitness.com", NormalizedUserName = "ROBERT.WILSON@CARVEDROCKFITNESS.COM", Email = "robert.wilson@carvedrockfitness.com", NormalizedEmail="ROBERT.WILSON@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-7", ConcurrencyStamp = "static-concurrency-7", Role = "Trainee", TrainerId = 3 },
            new User { Id = 8, Name = "Linda Martinez", UserName = "linda.martinez@carvedrockfitness.com", NormalizedUserName = "LINDA.MARTINEZ@CARVEDROCKFITNESS.COM", Email = "linda.martinez@carvedrockfitness.com", NormalizedEmail="LINDA.MARTINEZ@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-8", ConcurrencyStamp = "static-concurrency-8", Role = "Trainee", TrainerId = 3 },
            new User { Id = 9, Name = "David Anderson", UserName = "david.anderson@carvedrockfitness.com", NormalizedUserName = "DAVID.ANDERSON@CARVEDROCKFITNESS.COM", Email = "david.anderson@carvedrockfitness.com", NormalizedEmail="DAVID.ANDERSON@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-9", ConcurrencyStamp = "static-concurrency-9", Role = "Trainee", TrainerId = 2 },
            new User { Id = 10, Name = "Sarah Thomas", UserName = "sarah.thomas@carvedrockfitness.com", NormalizedUserName = "SARAH.THOMAS@CARVEDROCKFITNESS.COM", Email = "sarah.thomas@carvedrockfitness.com", NormalizedEmail="SARAH.THOMAS@CARVEDROCKFITNESS.COM", EmailConfirmed = true, SecurityStamp = "static-stamp-10", ConcurrencyStamp = "static-concurrency-10", Role = "Trainee", TrainerId = 2 }
        );

        modelBuilder.Entity<AdBanner>().HasData(
            new AdBanner { Id = 1, BusinessName = "Tembisa Sports Gear",    ImageUrl = "https://placehold.co/800x200/FF6B00/FFFFFF?text=Tembisa+Sports+Gear",    LinkUrl = "#", TagLine = "Boots, kits & balls — delivered to your door",     IsActive = true, IsDemoData = true, CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AdBanner { Id = 2, BusinessName = "Soweto Shisanyama",       ImageUrl = "https://placehold.co/800x200/1A1A2E/FF6B00?text=Soweto+Shisanyama",       LinkUrl = "#", TagLine = "After the game, celebrate with us — Alexandra",   IsActive = true, IsDemoData = true, CreatedAt = new DateTime(2025, 3, 2, 0, 0, 0, DateTimeKind.Utc) },
            new AdBanner { Id = 3, BusinessName = "YVEDA Academy",           ImageUrl = "https://placehold.co/800x200/003366/FFD700?text=YVEDA+Elite+Development", LinkUrl = "#", TagLine = "Building champions in sport. Shaping leaders in life.", IsActive = true, IsDemoData = true, CreatedAt = new DateTime(2025, 3, 3, 0, 0, 0, DateTimeKind.Utc) },
            new AdBanner { Id = 4, BusinessName = "Capitec Bank",            ImageUrl = "https://placehold.co/800x200/007B5E/FFFFFF?text=Capitec+Bank",            LinkUrl = "#", TagLine = "Pay your sport fees instantly with Capitec Pay",   IsActive = true, IsDemoData = true, CreatedAt = new DateTime(2025, 3, 4, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Suggestion>().HasData(
            new Suggestion { Id = 1,  IsDemoData = true, Title = "Morning HIIT at CityRock",           Description = "High intensity interval training for all levels. 45 min sweat session — bring water and a towel. Coach-led, limited to 20 people.",                                            Category = "HIIT",      Location = "Cape Town, Woodstock",       CreatedAt = new DateTime(2025, 3,  1,  6,  0, 0, DateTimeKind.Utc), CreatedByName = "John Doe" },
            new Suggestion { Id = 2,  IsDemoData = true, Title = "Sunday Yoga & Mobility Flow",         Description = "Slow-paced yoga and mobility work. Perfect active recovery for athletes. All levels welcome, mats provided.",                                                               Category = "Yoga",      Location = "Johannesburg, Sandton",      CreatedAt = new DateTime(2025, 3,  5,  7, 30, 0, DateTimeKind.Utc), CreatedByName = "Jane Smith" },
            new Suggestion { Id = 3,  IsDemoData = true, Title = "Powerlifting Club — Weekly Meet",     Description = "Squat, bench and deadlift coaching every Thursday evening. Spotters available. Bring your programme and let's grind.",                                                       Category = "Strength",  Location = "Durban, Umhlanga",           CreatedAt = new DateTime(2025, 3,  8, 18,  0, 0, DateTimeKind.Utc), CreatedByName = "Mike Johnson" },
            new Suggestion { Id = 4,  IsDemoData = true, Title = "5km Park Run Social Group",           Description = "Friendly running group every Saturday at 7am. All paces welcome — from first-timers to 20-minute runners. We grab coffee after.",                                           Category = "Running",   Location = "Pretoria, Groenkloof",       CreatedAt = new DateTime(2025, 3, 10,  7,  0, 0, DateTimeKind.Utc), CreatedByName = "John Doe" },
            new Suggestion { Id = 5,  IsDemoData = true, Title = "Boxing Conditioning Class",           Description = "Cardio boxing with bag work and partner drills. No experience needed. Get fit, relieve stress, meet great people.",                                                          Category = "Boxing",    Location = "Cape Town, Green Point",     CreatedAt = new DateTime(2025, 3, 12, 17,  0, 0, DateTimeKind.Utc), CreatedByName = "Jane Smith" },
            new Suggestion { Id = 6,  IsDemoData = true, Title = "CrossFit Open Community WOD",         Description = "Open community workout of the day. All athletes welcome regardless of level. Scaled and RX options available. Let's suffer together!",                                       Category = "CrossFit",  Location = "Johannesburg, Fourways",     CreatedAt = new DateTime(2025, 3, 15,  6,  0, 0, DateTimeKind.Utc), CreatedByName = "Mike Johnson" },
            new Suggestion { Id = 7,  IsDemoData = true, Title = "Sunday League Football — Soweto",     Description = "Weekly Sunday league for ages 16+. 11-a-side, kick-off at 9am. Bring your boots and your game. All skill levels welcome. Teams of 11 or join as a free agent.",             Category = "Football",  Location = "Soweto, Meadowlands",        CreatedAt = new DateTime(2025, 3, 20,  8,  0, 0, DateTimeKind.Utc), CreatedByName = "John Doe" },
            new Suggestion { Id = 8,  IsDemoData = true, Title = "5-a-Side Football — Alexandra",       Description = "Friday evening 5-a-side at the Alex community pitch. R20 per player. Goalkeepers especially needed. WhatsApp to confirm your spot before 2pm on Friday.",                  Category = "Football",  Location = "Alexandra, Johannesburg",    CreatedAt = new DateTime(2025, 3, 22, 16,  0, 0, DateTimeKind.Utc), CreatedByName = "Jane Smith" },
            new Suggestion { Id = 9,  IsDemoData = true, Title = "Township Cricket League — Tembisa",   Description = "Saturday morning cricket league for adults. Batting, bowling and fielding coaching included. Bats and pads provided for new players. Competitive and friendly atmosphere.",   Category = "Cricket",   Location = "Tembisa, Kempton Park",      CreatedAt = new DateTime(2025, 3, 24,  8,  0, 0, DateTimeKind.Utc), CreatedByName = "Mike Johnson" },
            new Suggestion { Id = 10, IsDemoData = true, Title = "Morning Cycling Club — Pretoria East",Description = "Road cycling group every Saturday 6am. Routes from 30km to 80km. All road bikes welcome. Safety briefing before each ride. Coffee stop at 40km mark.",                       Category = "Cycling",   Location = "Pretoria East, Garsfontein", CreatedAt = new DateTime(2025, 3, 26,  6,  0, 0, DateTimeKind.Utc), CreatedByName = "John Doe" },
            new Suggestion { Id = 11, IsDemoData = true, Title = "Kids Tennis Coaching — Sandton",      Description = "Tennis lessons for beginners aged 8–16. Qualified coach with 10 years experience. Rackets provided. Small groups of max 6 kids. Saturdays 10am–12pm.",                       Category = "Tennis",    Location = "Sandton, Johannesburg",      CreatedAt = new DateTime(2025, 3, 28, 10,  0, 0, DateTimeKind.Utc), CreatedByName = "Jane Smith" },
            new Suggestion { Id = 12, IsDemoData = true, Title = "Kids Transport — Tembisa to YVEDA",   Description = "Reliable transport for kids to SA Young Vision Elite Development Academy (YVEDA). Tembisa pickup routes available. R300/month. Verified driver, police clearance held. WhatsApp to book.", Category = "Transport", Location = "Tembisa, Kempton Park",  CreatedAt = new DateTime(2025, 3, 30,  7,  0, 0, DateTimeKind.Utc), CreatedByName = "Mike Johnson" }
        );

        modelBuilder.Entity<AcademyRegistration>().HasData(
            new AcademyRegistration { Id = 1,  IsDemoData = true, PlayerName = "Sipho Dlamini",     Age = 12, Sport = "Football",  ParentName = "Nomsa Dlamini",    ParentEmail = "nomsa@email.com",   ParentPhone = "083 111 2001", NeedsTransport = true,  PickupAddress = "Tembisa CBD",  Status = "Active",    AssignedRoute = "Tembisa CBD",  CreatedAt = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 2,  IsDemoData = true, PlayerName = "Thabo Mokoena",    Age = 14, Sport = "Football",  ParentName = "Grace Mokoena",    ParentEmail = "grace@email.com",   ParentPhone = "072 222 3002", NeedsTransport = true,  PickupAddress = "Ivory Park",   Status = "Active",    AssignedRoute = "Ivory Park",   CreatedAt = new DateTime(2025, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 3,  IsDemoData = true, PlayerName = "Lerato Khumalo",   Age = 10, Sport = "Cricket",   ParentName = "Peter Khumalo",    ParentEmail = "peter@email.com",   ParentPhone = "061 333 4003", NeedsTransport = false, Status = "Active",    CreatedAt = new DateTime(2025, 2, 5, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 4,  IsDemoData = true, PlayerName = "Naledi Sithole",   Age = 11, Sport = "Athletics", ParentName = "Maria Sithole",    ParentEmail = "maria@email.com",   ParentPhone = "082 444 5004", NeedsTransport = true,  PickupAddress = "Kwa-Thema",    Status = "Active",    AssignedRoute = "Kwa-Thema",    CreatedAt = new DateTime(2025, 2, 8, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 5,  IsDemoData = true, PlayerName = "Kabelo Mthembu",   Age = 16, Sport = "Football",  ParentName = "James Mthembu",    ParentEmail = "james@email.com",   ParentPhone = "073 555 6005", NeedsTransport = true,  PickupAddress = "Rabie Ridge",  Status = "Active",    AssignedRoute = "Rabie Ridge",  CreatedAt = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 6,  IsDemoData = true, PlayerName = "Zanele Nkosi",     Age = 9,  Sport = "Tennis",    ParentName = "Ruth Nkosi",       ParentEmail = "ruth@email.com",    ParentPhone = "084 666 7006", NeedsTransport = false, Status = "Active",    CreatedAt = new DateTime(2025, 2, 12, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 7,  IsDemoData = true, PlayerName = "Bongani Zulu",     Age = 13, Sport = "Football",  ParentName = "David Zulu",       ParentEmail = "david@email.com",   ParentPhone = "065 777 8007", NeedsTransport = true,  PickupAddress = "Ebony Park",   Status = "Active",    AssignedRoute = "Ebony Park",   CreatedAt = new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 8,  IsDemoData = true, PlayerName = "Mpho Mahlangu",    Age = 15, Sport = "Cricket",   ParentName = "Sarah Mahlangu",   ParentEmail = "sarah@email.com",   ParentPhone = "071 888 9008", NeedsTransport = true,  PickupAddress = "Clayville",    Status = "Confirmed", AssignedRoute = "Clayville",    CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 9,  IsDemoData = true, PlayerName = "Ayanda Ndlovu",    Age = 8,  Sport = "Athletics", ParentName = "Linda Ndlovu",     ParentEmail = "linda@email.com",   ParentPhone = "062 999 1009", NeedsTransport = false, Status = "Pending",   CreatedAt = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 10, IsDemoData = true, PlayerName = "Tshepo Molefe",    Age = 17, Sport = "Football",  ParentName = "Anna Molefe",      ParentEmail = "anna@email.com",    ParentPhone = "083 101 2010", NeedsTransport = true,  PickupAddress = "Tembisa CBD",  Status = "Active",    AssignedRoute = "Tembisa CBD",  CreatedAt = new DateTime(2025, 3, 8, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 11, IsDemoData = true, PlayerName = "Palesa Maseko",    Age = 11, Sport = "Tennis",    ParentName = "Joseph Maseko",    ParentEmail = "joseph@email.com",  ParentPhone = "074 202 3011", NeedsTransport = false, Status = "Active",    CreatedAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc) },
            new AcademyRegistration { Id = 12, IsDemoData = true, PlayerName = "Siyabonga Cele",   Age = 14, Sport = "Cricket",   ParentName = "Thandi Cele",      ParentEmail = "thandi@email.com",  ParentPhone = "081 303 4012", NeedsTransport = true,  PickupAddress = "Ivory Park",   Status = "Active",    AssignedRoute = "Ivory Park",   CreatedAt = new DateTime(2025, 3, 12, 0, 0, 0, DateTimeKind.Utc) }
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
