using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFormatoCampeonato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Formato",
                table: "FutPlay_Campeonatos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PontosCorridos");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Campeonatos_Formato",
                table: "FutPlay_Campeonatos",
                column: "Formato");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Campeonatos_Formato",
                table: "FutPlay_Campeonatos");

            migrationBuilder.DropColumn(
                name: "Formato",
                table: "FutPlay_Campeonatos");
        }
    }
}
