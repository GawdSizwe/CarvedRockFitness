using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarvedRockFitness.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_LatLng : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Suggestions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Suggestions",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Suggestions");
        }
    }
}
