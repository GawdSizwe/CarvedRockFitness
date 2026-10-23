using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarvedRockFitness.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademyConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGivenAt",
                table: "AcademyRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 4,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 5,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 6,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 7,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 8,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 9,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 10,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 11,
                column: "ConsentGivenAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "AcademyRegistrations",
                keyColumn: "Id",
                keyValue: 12,
                column: "ConsentGivenAt",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentGivenAt",
                table: "AcademyRegistrations");
        }
    }
}
