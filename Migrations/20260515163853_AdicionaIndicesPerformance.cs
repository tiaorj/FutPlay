using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIndicesPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Times_ApiTeamId",
                table: "FutPlay_Times",
                column: "ApiTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaParticipantes_Email",
                table: "FutPlay_LigaParticipantes",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Jogos_ApiFixtureId",
                table: "FutPlay_Jogos",
                column: "ApiFixtureId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Jogos_Status",
                table: "FutPlay_Jogos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Campeonatos_ApiLeagueId",
                table: "FutPlay_Campeonatos",
                column: "ApiLeagueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Times_ApiTeamId",
                table: "FutPlay_Times");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_LigaParticipantes_Email",
                table: "FutPlay_LigaParticipantes");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Jogos_ApiFixtureId",
                table: "FutPlay_Jogos");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Jogos_Status",
                table: "FutPlay_Jogos");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Campeonatos_ApiLeagueId",
                table: "FutPlay_Campeonatos");
        }
    }
}
