using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class Palpite
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A liga é obrigatória.")]
        [Display(Name = "Liga")]
        public int LigaId { get; set; }

        [ForeignKey("LigaId")]
        public Liga? Liga { get; set; }

        [Required(ErrorMessage = "O participante é obrigatório.")]
        [Display(Name = "Participante")]
        public int LigaParticipanteId { get; set; }

        [ForeignKey("LigaParticipanteId")]
        public LigaParticipante? LigaParticipante { get; set; }

        [Required(ErrorMessage = "O jogo é obrigatório.")]
        [Display(Name = "Jogo")]
        public int JogoId { get; set; }

        [ForeignKey("JogoId")]
        public Jogo? Jogo { get; set; }

        [Required(ErrorMessage = "Informe os gols do time da casa.")]
        [Display(Name = "Gols Casa")]
        [Range(0, 50, ErrorMessage = "Informe um valor entre 0 e 50.")]
        public int GolsCasaPalpite { get; set; }

        [Required(ErrorMessage = "Informe os gols do time visitante.")]
        [Display(Name = "Gols Visitante")]
        [Range(0, 50, ErrorMessage = "Informe um valor entre 0 e 50.")]
        public int GolsVisitantePalpite { get; set; }

        [Display(Name = "Data do Palpite")]
        public DateTime DataPalpite { get; set; } = DateTime.Now;

        [Display(Name = "Pontos Ganhos")]
        public int PontosGanhos { get; set; } = 0;

        public bool Ativo { get; set; } = true;
    }
}