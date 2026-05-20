using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class Classificacao
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Campeonato")]
        public int CampeonatoId { get; set; }

        [ForeignKey("CampeonatoId")]
        public Campeonato? Campeonato { get; set; }

        [Required]
        [Display(Name = "Time")]
        public int TimeId { get; set; }

        [ForeignKey("TimeId")]
        public Time? Time { get; set; }

        public int Posicao { get; set; }

        public int Pontos { get; set; }

        public int Jogos { get; set; }

        public int Vitorias { get; set; }

        public int Empates { get; set; }

        public int Derrotas { get; set; }

        public int GolsPro { get; set; }

        public int GolsContra { get; set; }

        public int SaldoGols { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
