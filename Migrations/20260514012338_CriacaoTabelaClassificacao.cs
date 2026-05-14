using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutPlay.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoTabelaClassificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FutPlay_Classificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampeonatoId = table.Column<int>(type: "int", nullable: false),
                    TimeId = table.Column<int>(type: "int", nullable: false),
                    Posicao = table.Column<int>(type: "int", nullable: false),
                    Pontos = table.Column<int>(type: "int", nullable: false),
                    Jogos = table.Column<int>(type: "int", nullable: false),
                    Vitorias = table.Column<int>(type: "int", nullable: false),
                    Empates = table.Column<int>(type: "int", nullable: false),
                    Derrotas = table.Column<int>(type: "int", nullable: false),
                    GolsPro = table.Column<int>(type: "int", nullable: false),
                    GolsContra = table.Column<int>(type: "int", nullable: false),
                    SaldoGols = table.Column<int>(type: "int", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FutPlay_Classificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FutPlay_Classificacoes_FutPlay_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "FutPlay_Campeonatos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FutPlay_Classificacoes_FutPlay_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "FutPlay_Times",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Classificacoes_CampeonatoId",
                table: "FutPlay_Classificacoes",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_FutPlay_Classificacoes_TimeId",
                table: "FutPlay_Classificacoes",
                column: "TimeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FutPlay_Classificacoes");
        }
    }
}
