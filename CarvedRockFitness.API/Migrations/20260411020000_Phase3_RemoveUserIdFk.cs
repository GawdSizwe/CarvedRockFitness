using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarvedRockFitness.API.Migrations
{
    public partial class Phase3_RemoveUserIdFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suggestions_AspNetUsers_UserId",
                table: "Suggestions");

            migrationBuilder.DropIndex(
                name: "IX_Suggestions_UserId",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Suggestions");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "Suggestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Suggestions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedByEmail", table: "Suggestions");
            migrationBuilder.DropColumn(name: "CreatedByName", table: "Suggestions");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Suggestions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
