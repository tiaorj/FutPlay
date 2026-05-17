using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriaTabelaLigaConvitesAtualiza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_LigaConvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LigaId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NomeConvidado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokenConvite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoConvite = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataAceite = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserIdAceite = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_LigaConvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_LigaConvites_AspNetUsers_UserIdAceite",
                        column: x => x.UserIdAceite,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_LigaConvites_FutPlay_Ligas_LigaId",
                        column: x => x.LigaId,
                        principalTable: "FutPlay_Ligas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaConvites_Email",
                table: "FutPlay_LigaConvites",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaConvites_LigaId",
                table: "FutPlay_LigaConvites",
                column: "LigaId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaConvites_Status",
                table: "FutPlay_LigaConvites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaConvites_TokenConvite",
                table: "FutPlay_LigaConvites",
                column: "TokenConvite",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_LigaConvites_UserIdAceite",
                table: "FutPlay_LigaConvites",
                column: "UserIdAceite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_LigaConvites");
        }
    }
}
