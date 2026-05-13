using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class LigaParticipante
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A liga é obrigatória.")]
        [Display(Name = "Liga")]
        public int LigaId { get; set; }

        [ForeignKey("LigaId")]
        public Liga? Liga { get; set; }

        [Required(ErrorMessage = "O nome do participante é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Data de Entrada")]
        public DateTime DataEntrada { get; set; } = DateTime.Now;

        [Display(Name = "Pontuação Total")]
        public int PontuacaoTotal { get; set; } = 0;

        public bool Ativo { get; set; } = true;
    }
}