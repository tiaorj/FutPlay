using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoTabelaPalpites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_Palpites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LigaId = table.Column<int>(type: "int", nullable: false),
                    LigaParticipanteId = table.Column<int>(type: "int", nullable: false),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    GolsCasaPalpite = table.Column<int>(type: "int", nullable: false),
                    GolsVisitantePalpite = table.Column<int>(type: "int", nullable: false),
                    DataPalpite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PontosGanhos = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_Palpites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_Palpites_FutPlay_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "FutPlay_Jogos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_Palpites_FutPlay_LigaParticipantes_LigaParticipanteId",
                        column: x => x.LigaParticipanteId,
                        principalTable: "FutPlay_LigaParticipantes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_Palpites_FutPlay_Ligas_LigaId",
                        column: x => x.LigaId,
                        principalTable: "FutPlay_Ligas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Palpites_JogoId",
                table: "FutPlay_Palpites",
                column: "JogoId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Palpites_LigaId",
                table: "FutPlay_Palpites",
                column: "LigaId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Palpites_LigaParticipanteId",
                table: "FutPlay_Palpites",
                column: "LigaParticipanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_Palpites");
        }
    }
}
