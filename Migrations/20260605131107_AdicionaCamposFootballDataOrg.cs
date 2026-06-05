using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCamposFootballDataOrg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FootballDataMatchId",
                table: "FutPlay_Jogos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FootballDataCompetitionCode",
                table: "FutPlay_Campeonatos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FootballDataCompetitionId",
                table: "FutPlay_Campeonatos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FootballDataSeason",
                table: "FutPlay_Campeonatos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FootballDataCompetitionId",
                table: "FutPlay_ApiSyncLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FootballDataMatchId",
                table: "FutPlay_ApiSyncLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Jogos_FootballDataMatchId",
                table: "FutPlay_Jogos",
                column: "FootballDataMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Campeonatos_FootballDataCompetitionCode",
                table: "FutPlay_Campeonatos",
                column: "FootballDataCompetitionCode");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Campeonatos_FootballDataCompetitionId",
                table: "FutPlay_Campeonatos",
                column: "FootballDataCompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Campeonatos_FootballDataSeason",
                table: "FutPlay_Campeonatos",
                column: "FootballDataSeason");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_FootballDataCompetitionId",
                table: "FutPlay_ApiSyncLogs",
                column: "FootballDataCompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_FootballDataMatchId",
                table: "FutPlay_ApiSyncLogs",
                column: "FootballDataMatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Jogos_FootballDataMatchId",
                table: "FutPlay_Jogos");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Campeonatos_FootballDataCompetitionCode",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Campeonatos_FootballDataCompetitionId",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Campeonatos_FootballDataSeason",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_ApiSyncLogs_FootballDataCompetitionId",
                table: "FutPlay_ApiSyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_ApiSyncLogs_FootballDataMatchId",
                table: "FutPlay_ApiSyncLogs");

            migrationBuilder.DropColumn(
                name: "FootballDataMatchId",
                table: "FutPlay_Jogos");

            migrationBuilder.DropColumn(
                name: "FootballDataCompetitionCode",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropColumn(
                name: "FootballDataCompetitionId",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropColumn(
                name: "FootballDataSeason",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropColumn(
                name: "FootballDataCompetitionId",
                table: "FutPlay_ApiSyncLogs");

            migrationBuilder.DropColumn(
                name: "FootballDataMatchId",
                table: "FutPlay_ApiSyncLogs");
        }
    }
}
