using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarvedRockFitness.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademyRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDemoData",
                table: "Suggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AcademyRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Sport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedsTransport = table.Column<bool>(type: "bit", nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedRoute = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDemoData = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdBanners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagLine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDemoData = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdBanners", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AcademyRegistrations",
                columns: new[] { "Id", "Age", "AssignedRoute", "CreatedAt", "IsDemoData", "NeedsTransport", "Notes", "ParentEmail", "ParentName", "ParentPhone", "PickupAddress", "PlayerName", "Sport", "Status" },
                values: new object[,]
                {
                    { 1, 12, "Tembisa CBD", new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "nomsa@email.com", "Nomsa Dlamini", "083 111 2001", "Tembisa CBD", "Sipho Dlamini", "Football", "Active" },
                    { 2, 14, "Ivory Park", new DateTime(2025, 2, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "grace@email.com", "Grace Mokoena", "072 222 3002", "Ivory Park", "Thabo Mokoena", "Football", "Active" },
                    { 3, 10, null, new DateTime(2025, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, "peter@email.com", "Peter Khumalo", "061 333 4003", null, "Lerato Khumalo", "Cricket", "Active" },
                    { 4, 11, "Kwa-Thema", new DateTime(2025, 2, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "maria@email.com", "Maria Sithole", "082 444 5004", "Kwa-Thema", "Naledi Sithole", "Athletics", "Active" },
                    { 5, 16, "Rabie Ridge", new DateTime(2025, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "james@email.com", "James Mthembu", "073 555 6005", "Rabie Ridge", "Kabelo Mthembu", "Football", "Active" },
                    { 6, 9, null, new DateTime(2025, 2, 12, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, "ruth@email.com", "Ruth Nkosi", "084 666 7006", null, "Zanele Nkosi", "Tennis", "Active" },
                    { 7, 13, "Ebony Park", new DateTime(2025, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "david@email.com", "David Zulu", "065 777 8007", "Ebony Park", "Bongani Zulu", "Football", "Active" },
                    { 8, 15, "Clayville", new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "sarah@email.com", "Sarah Mahlangu", "071 888 9008", "Clayville", "Mpho Mahlangu", "Cricket", "Confirmed" },
                    { 9, 8, null, new DateTime(2025, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, "linda@email.com", "Linda Ndlovu", "062 999 1009", null, "Ayanda Ndlovu", "Athletics", "Pending" },
                    { 10, 17, "Tembisa CBD", new DateTime(2025, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "anna@email.com", "Anna Molefe", "083 101 2010", "Tembisa CBD", "Tshepo Molefe", "Football", "Active" },
                    { 11, 11, null, new DateTime(2025, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, "joseph@email.com", "Joseph Maseko", "074 202 3011", null, "Palesa Maseko", "Tennis", "Active" },
                    { 12, 14, "Ivory Park", new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, "thandi@email.com", "Thandi Cele", "081 303 4012", "Ivory Park", "Siyabonga Cele", "Cricket", "Active" }
                });

            migrationBuilder.InsertData(
                table: "AdBanners",
                columns: new[] { "Id", "BusinessName", "CreatedAt", "ImageUrl", "IsActive", "IsDemoData", "LinkUrl", "TagLine" },
                values: new object[,]
                {
                    { 1, "Tembisa Sports Gear", new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://placehold.co/800x200/FF6B00/FFFFFF?text=Tembisa+Sports+Gear", true, true, "#", "Boots, kits & balls — delivered to your door" },
                    { 2, "Soweto Shisanyama", new DateTime(2025, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "https://placehold.co/800x200/1A1A2E/FF6B00?text=Soweto+Shisanyama", true, true, "#", "After the game, celebrate with us — Alexandra" },
                    { 3, "YVEDA Academy", new DateTime(2025, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "https://placehold.co/800x200/003366/FFD700?text=YVEDA+Elite+Development", true, true, "#", "Building champions in sport. Shaping leaders in life." },
                    { 4, "Capitec Bank", new DateTime(2025, 3, 4, 0, 0, 0, 0, DateTimeKind.Utc), "https://placehold.co/800x200/007B5E/FFFFFF?text=Capitec+Bank", true, true, "#", "Pay your sport fees instantly with Capitec Pay" }
                });

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 9,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 10,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 11,
                column: "IsDemoData",
                value: true);

            migrationBuilder.UpdateData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 12,
                column: "IsDemoData",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademyRegistrations");

            migrationBuilder.DropTable(
                name: "AdBanners");

            migrationBuilder.DropColumn(
                name: "IsDemoData",
                table: "Suggestions");
        }
    }
}
