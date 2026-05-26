using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriaPalpitesComunidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_PalpitesComunidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JogoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ResultadoPrevisto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GolsCasaPalpite = table.Column<int>(type: "int", nullable: true),
                    GolsVisitantePalpite = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_PalpitesComunidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_PalpitesComunidade_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FutPlay_PalpitesComunidade_FutPlay_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "FutPlay_Jogos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_PalpitesComunidade_ResultadoPrevisto",
                table: "FutPlay_PalpitesComunidade",
                column: "ResultadoPrevisto");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_PalpitesComunidade_UsuarioId",
                table: "FutPlay_PalpitesComunidade",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_FutPlay_PalpitesComunidade_Jogo_Usuario",
                table: "FutPlay_PalpitesComunidade",
                columns: new[] { "JogoId", "UsuarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_PalpitesComunidade");
        }
    }
}
