using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCamposApiFootball : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApiTeamId",
                table: "FutPlay_Times",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApiLeagueId",
                table: "FutPlay_Campeonatos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiTeamId",
                table: "FutPlay_Times");

            migrationBuilder.DropColumn(
                name: "ApiLeagueId",
                table: "FutPlay_Campeonatos");
        }
    }
}
