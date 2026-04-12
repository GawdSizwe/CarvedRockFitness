using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarvedRockFitness.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Suggestions",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ImagePath", "Location", "Title", "UserId" },
                values: new object[,]
                {
                    { 1, "HIIT", new DateTime(2025, 3, 1, 6, 0, 0, 0, DateTimeKind.Utc), "High intensity interval training for all levels. 45 min sweat session — bring water and a towel. Coach-led, limited to 20 people.", null, "Cape Town, Woodstock", "Morning HIIT at CityRock", 1 },
                    { 2, "Yoga", new DateTime(2025, 3, 5, 7, 30, 0, 0, DateTimeKind.Utc), "Slow-paced yoga and mobility work. Perfect active recovery for athletes. All levels welcome, mats provided.", null, "Johannesburg, Sandton", "Sunday Yoga & Mobility Flow", 2 },
                    { 3, "Strength", new DateTime(2025, 3, 8, 18, 0, 0, 0, DateTimeKind.Utc), "Squat, bench and deadlift coaching every Thursday evening. Spotters available. Bring your programme and let's grind.", null, "Durban, Umhlanga", "Powerlifting Club — Weekly Meet", 3 },
                    { 4, "Running", new DateTime(2025, 3, 10, 7, 0, 0, 0, DateTimeKind.Utc), "Friendly running group every Saturday at 7am. All paces welcome — from first-timers to 20-minute runners. We grab coffee after.", null, "Pretoria, Groenkloof", "5km Park Run Social Group", 1 },
                    { 5, "Boxing", new DateTime(2025, 3, 12, 17, 0, 0, 0, DateTimeKind.Utc), "Cardio boxing with bag work and partner drills. No experience needed. Get fit, relieve stress, meet great people.", null, "Cape Town, Green Point", "Boxing Conditioning Class", 2 },
                    { 6, "CrossFit", new DateTime(2025, 3, 15, 6, 0, 0, 0, DateTimeKind.Utc), "Open community workout of the day. All athletes welcome regardless of level. Scaled and RX options available. Let's suffer together!", null, "Johannesburg, Fourways", "CrossFit Open Community WOD", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
