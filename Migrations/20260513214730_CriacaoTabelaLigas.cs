using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoTabelaLigas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_Ligas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CampeonatoId = table.Column<int>(type: "int", nullable: false),
                    CodigoConvite = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Publica = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_Ligas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_Ligas_FutPlay_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "FutPlay_Campeonatos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Ligas_CampeonatoId",
                table: "FutPlay_Ligas",
                column: "CampeonatoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_Ligas");
        }
    }
}
