using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCriadorLiga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CriadorUserId",
                table: "FutPlay_Ligas",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Ligas_CriadorUserId",
                table: "FutPlay_Ligas",
                column: "CriadorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FutPlay_Ligas_AspNetUsers_CriadorUserId",
                table: "FutPlay_Ligas",
                column: "CriadorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FutPlay_Ligas_AspNetUsers_CriadorUserId",
                table: "FutPlay_Ligas");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_Ligas_CriadorUserId",
                table: "FutPlay_Ligas");

            migrationBuilder.DropColumn(
                name: "CriadorUserId",
                table: "FutPlay_Ligas");
        }
    }
}
