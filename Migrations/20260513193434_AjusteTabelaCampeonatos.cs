using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AjusteTabelaCampeonatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Campeonatos",
                table: "Campeonatos");

            migrationBuilder.RenameTable(
                name: "Campeonatos",
                newName: "FutPlay_Campeonatos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FutPlay_Campeonatos",
                table: "FutPlay_Campeonatos",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FutPlay_Campeonatos",
                table: "FutPlay_Campeonatos");

            migrationBuilder.RenameTable(
                name: "FutPlay_Campeonatos",
                newName: "Campeonatos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Campeonatos",
                table: "Campeonatos",
                column: "Id");
        }
    }
}
