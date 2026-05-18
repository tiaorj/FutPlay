using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFavoritosUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_CampeonatoFavoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CampeonatoId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_CampeonatoFavoritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_CampeonatoFavoritos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FutPlay_CampeonatoFavoritos_FutPlay_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "FutPlay_Campeonatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FutPlay_TimeFavoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TimeId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_TimeFavoritos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_TimeFavoritos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FutPlay_TimeFavoritos_FutPlay_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "FutPlay_Times",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_CampeonatoFavoritos_CampeonatoId",
                table: "FutPlay_CampeonatoFavoritos",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_CampeonatoFavoritos_UserId_CampeonatoId",
                table: "FutPlay_CampeonatoFavoritos",
                columns: new[] { "UserId", "CampeonatoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_TimeFavoritos_TimeId",
                table: "FutPlay_TimeFavoritos",
                column: "TimeId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_TimeFavoritos_UserId_TimeId",
                table: "FutPlay_TimeFavoritos",
                columns: new[] { "UserId", "TimeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_CampeonatoFavoritos");

            migrationBuilder.DropTable(
                name: "FutPlay_TimeFavoritos");
        }
    }
}
