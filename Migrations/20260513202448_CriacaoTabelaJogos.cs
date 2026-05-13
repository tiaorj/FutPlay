using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoTabelaJogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_Jogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampeonatoId = table.Column<int>(type: "int", nullable: false),
                    TimeCasaId = table.Column<int>(type: "int", nullable: false),
                    TimeVisitanteId = table.Column<int>(type: "int", nullable: false),
                    DataJogo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Grupo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Rodada = table.Column<int>(type: "int", nullable: true),
                    GolsCasa = table.Column<int>(type: "int", nullable: true),
                    GolsVisitante = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_Jogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_Jogos_FutPlay_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "FutPlay_Campeonatos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_Jogos_FutPlay_Times_TimeCasaId",
                        column: x => x.TimeCasaId,
                        principalTable: "FutPlay_Times",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_Jogos_FutPlay_Times_TimeVisitanteId",
                        column: x => x.TimeVisitanteId,
                        principalTable: "FutPlay_Times",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Jogos_CampeonatoId",
                table: "FutPlay_Jogos",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Jogos_TimeCasaId",
                table: "FutPlay_Jogos",
                column: "TimeCasaId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Jogos_TimeVisitanteId",
                table: "FutPlay_Jogos",
                column: "TimeVisitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_Jogos");
        }
    }
}
