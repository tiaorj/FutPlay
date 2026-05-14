using FutPlay.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260514170000_AdicionaUserIdLigaParticipante")]
    public partial class AdicionaUserIdLigaParticipante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FutPlay_LigaParticipantes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaParticipantes_UserId",
                table: "FutPlay_LigaParticipantes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FutPlay_LigaParticipantes_AspNetUsers_UserId",
                table: "FutPlay_LigaParticipantes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FutPlay_LigaParticipantes_AspNetUsers_UserId",
                table: "FutPlay_LigaParticipantes");

            migrationBuilder.DropIndex(
                name: "IX_FutPlay_LigaParticipantes_UserId",
                table: "FutPlay_LigaParticipantes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FutPlay_LigaParticipantes");
        }
    }
}
