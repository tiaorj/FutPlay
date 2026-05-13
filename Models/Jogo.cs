using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class Jogo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O campeonato é obrigatório.")]
        [Display(Name = "Campeonato")]
        public int CampeonatoId { get; set; }

        [ForeignKey("CampeonatoId")]
        public Campeonato? Campeonato { get; set; }

        [Required(ErrorMessage = "O time da casa é obrigatório.")]
        [Display(Name = "Time da Casa")]
        public int TimeCasaId { get; set; }

        [ForeignKey("TimeCasaId")]
        public Time? TimeCasa { get; set; }

        [Required(ErrorMessage = "O time visitante é obrigatório.")]
        [Display(Name = "Time Visitante")]
        public int TimeVisitanteId { get; set; }

        [ForeignKey("TimeVisitanteId")]
        public Time? TimeVisitante { get; set; }

        [Required(ErrorMessage = "A data do jogo é obrigatória.")]
        [Display(Name = "Data do Jogo")]
        public DateTime DataJogo { get; set; }

        [StringLength(50)]
        public string? Fase { get; set; }

        [StringLength(20)]
        public string? Grupo { get; set; }

        public int? Rodada { get; set; }

        [Display(Name = "Gols Casa")]
        public int? GolsCasa { get; set; }

        [Display(Name = "Gols Visitante")]
        public int? GolsVisitante { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Agendado";

        public bool Ativo { get; set; } = true;
    }
}