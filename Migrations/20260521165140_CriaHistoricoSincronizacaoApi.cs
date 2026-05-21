using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriaHistoricoSincronizacaoApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_ApiSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoSincronizacao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CampeonatoId = table.Column<int>(type: "int", nullable: true),
                    TimeId = table.Column<int>(type: "int", nullable: true),
                    ApiLeagueId = table.Column<int>(type: "int", nullable: true),
                    ApiTeamId = table.Column<int>(type: "int", nullable: true),
                    ApiFixtureId = table.Column<int>(type: "int", nullable: true),
                    Temporada = table.Column<int>(type: "int", nullable: true),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalProcessados = table.Column<int>(type: "int", nullable: false),
                    TotalCriados = table.Column<int>(type: "int", nullable: false),
                    TotalAtualizados = table.Column<int>(type: "int", nullable: false),
                    TotalIgnorados = table.Column<int>(type: "int", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErroDetalhado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_ApiSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_ApiSyncLogs_FutPlay_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "FutPlay_Campeonatos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_ApiSyncLogs_FutPlay_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "FutPlay_Times",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_CampeonatoId",
                table: "FutPlay_ApiSyncLogs",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_CriadoEm",
                table: "FutPlay_ApiSyncLogs",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_Status",
                table: "FutPlay_ApiSyncLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_TimeId",
                table: "FutPlay_ApiSyncLogs",
                column: "TimeId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_ApiSyncLogs_TipoSincronizacao",
                table: "FutPlay_ApiSyncLogs",
                column: "TipoSincronizacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_ApiSyncLogs");
        }
    }
}
