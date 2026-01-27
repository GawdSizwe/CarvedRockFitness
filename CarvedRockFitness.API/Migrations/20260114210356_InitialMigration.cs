using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarvedRockFitness.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrainerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Users_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Routines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routines_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "Role", "TrainerId" },
                values: new object[,]
                {
                    { 1, "john.doe@carvedrockfitness.com", "John Doe", "Admin", null },
                    { 2, "jane.smith@carvedrockfitness.com", "Jane Smith", "Trainer", null },
                    { 3, "mike.johnson@carvedrockfitness.com", "Mike Johnson", "Trainer", null },
                    { 4, "emily.davis@carvedrockfitness.com", "Emily Davis", "Trainee", 2 },
                    { 5, "chris.brown@carvedrockfitness.com", "Chris Brown", "Trainee", 2 },
                    { 6, "patricia.taylor@carvedrockfitness.com", "Patricia Taylor", "Trainee", 3 },
                    { 7, "robert.wilson@carvedrockfitness.com", "Robert Wilson", "Trainee", 3 },
                    { 8, "linda.martinez@carvedrockfitness.com", "Linda Martinez", "Trainee", 3 },
                    { 9, "david.anderson@carvedrockfitness.com", "David Anderson", "Trainee", 2 },
                    { 10, "sarah.thomas@carvedrockfitness.com", "Sarah Thomas", "Trainee", 2 }
                });

            migrationBuilder.InsertData(
                table: "Routines",
                columns: new[] { "Id", "Description", "Name", "UserId" },
                values: new object[,]
                {
                    { 1, "A relaxing morning yoga routine", "Morning Yoga", 4 },
                    { 2, "High-intensity cardio workout", "Cardio Blast", 5 },
                    { 3, "Full-body strength training", "Strength Training", 6 },
                    { 4, "High-Intensity Interval Training", "HIIT Workout", 7 },
                    { 5, "Core strengthening Pilates routine", "Pilates", 8 },
                    { 6, "Gentle evening stretching routine", "Evening Stretch", 9 },
                    { 7, "Upper body strength training", "Upper Body Strength", 10 },
                    { 8, "Lower body strength training", "Lower Body Strength", 4 },
                    { 9, "Comprehensive full-body workout", "Full Body Workout", 5 },
                    { 10, "Intense core workout", "Core Workout", 6 },
                    { 11, "Flexibility and mobility exercises", "Flexibility Training", 7 },
                    { 12, "Balance and stability exercises", "Balance Training", 8 },
                    { 13, "Endurance and stamina building", "Endurance Training", 9 },
                    { 14, "Speed and agility drills", "Speed Training", 10 },
                    { 15, "Power and explosiveness training", "Power Training", 4 },
                    { 16, "Active recovery exercises", "Recovery Workout", 5 },
                    { 17, "Circuit-based workout routine", "Circuit Training", 6 },
                    { 18, "Bodyweight exercises", "Bodyweight Workout", 7 },
                    { 19, "Kettlebell exercises", "Kettlebell Workout", 8 },
                    { 20, "Dumbbell exercises", "Dumbbell Workout", 9 },
                    { 21, "Barbell exercises", "Barbell Workout", 10 },
                    { 22, "Resistance band exercises", "Resistance Band Workout", 4 },
                    { 23, "TRX suspension training", "TRX Workout", 5 },
                    { 24, "Plyometric exercises", "Plyometrics", 6 },
                    { 25, "Boxing and kickboxing drills", "Boxing Workout", 7 },
                    { 26, "Martial arts techniques", "Martial Arts Training", 8 },
                    { 27, "Dance-based fitness routine", "Dance Workout", 9 },
                    { 28, "Swimming drills and exercises", "Swimming Workout", 10 },
                    { 29, "Cycling drills and exercises", "Cycling Workout", 4 },
                    { 30, "Rowing drills and exercises", "Rowing Workout", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routines_UserId",
                table: "Routines",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TrainerId",
                table: "Users",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Routines");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
