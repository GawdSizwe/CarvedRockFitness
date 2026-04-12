using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarvedRockFitness.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_AdBanners_DemoFlag_EventDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdBanners",
                columns: table => new
                {
                    Id           = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ImageUrl     = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LinkUrl      = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TagLine      = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive     = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDemoData   = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdBanners", x => x.Id);
                });

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoData",
                table: "Suggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventDate",
                table: "Suggestions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Suggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Suggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Suggestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EventDate", "IsHidden", "IsPinned", "Priority" },
                values: new object[] { null, false, false, 0 });

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EventDate", "IsHidden", "IsPinned", "Priority" },
                values: new object[] { null, false, false, 0 });

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EventDate", "IsHidden", "IsPinned", "Priority" },
                values: new object[] { null, false, false, 0 });

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EventDate", "IsHidden", "IsPinned", "Priority" },
                values: new object[] { null, false, false, 0 });

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "EventDate", "IsHidden", "IsPinned", "Priority" },
                values: new object[] { null, false, false, 0 });

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "EventDate", "IsHidden", "IsPinned", "Priority" },
                values: new object[] { null, false, false, 0 });

            migrationBuilder.InsertData(
                table: "Suggestions",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "EventDate", "ImagePath", "IsHidden", "IsPinned", "Location", "Priority", "Title", "UserId" },
                values: new object[,]
                {
                    { 7, "Football", new DateTime(2025, 3, 20, 8, 0, 0, 0, DateTimeKind.Utc), "Weekly Sunday league for ages 16+. 11-a-side, kick-off at 9am. Bring your boots and your game. All skill levels welcome. Teams of 11 or join as a free agent.", null, null, false, false, "Soweto, Meadowlands", 0, "Sunday League Football — Soweto", 1 },
                    { 8, "Football", new DateTime(2025, 3, 22, 16, 0, 0, 0, DateTimeKind.Utc), "Friday evening 5-a-side at the Alex community pitch. R20 per player. Goalkeepers especially needed. WhatsApp to confirm your spot before 2pm on Friday.", null, null, false, false, "Alexandra, Johannesburg", 0, "5-a-Side Football — Alexandra", 2 },
                    { 9, "Cricket", new DateTime(2025, 3, 24, 8, 0, 0, 0, DateTimeKind.Utc), "Saturday morning cricket league for adults. Batting, bowling and fielding coaching included. Bats and pads provided for new players. Competitive and friendly atmosphere.", null, null, false, false, "Tembisa, Kempton Park", 0, "Township Cricket League — Tembisa", 3 },
                    { 10, "Cycling", new DateTime(2025, 3, 26, 6, 0, 0, 0, DateTimeKind.Utc), "Road cycling group every Saturday 6am. Routes from 30km to 80km. All road bikes welcome. Safety briefing before each ride. Coffee stop at 40km mark.", null, null, false, false, "Pretoria East, Garsfontein", 0, "Morning Cycling Club — Pretoria East", 1 },
                    { 11, "Tennis", new DateTime(2025, 3, 28, 10, 0, 0, 0, DateTimeKind.Utc), "Tennis lessons for beginners aged 8–16. Qualified coach with 10 years experience. Rackets provided. Small groups of max 6 kids. Saturdays 10am–12pm.", null, null, false, false, "Sandton, Johannesburg", 0, "Kids Tennis Coaching — Sandton", 2 },
                    { 12, "Transport", new DateTime(2025, 3, 30, 7, 0, 0, 0, DateTimeKind.Utc), "Reliable transport for kids to SA Young Vision Elite Development Academy (YVEDA). Tembisa pickup routes available. R300/month. Verified driver, police clearance held. WhatsApp to book.", null, null, false, false, "Tembisa, Kempton Park", 0, "Kids Transport — Tembisa to YVEDA", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DropTable(name: "AdBanners");

            migrationBuilder.DropColumn(
                name: "IsDemoData",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "EventDate",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Suggestions");
        }
    }
}
