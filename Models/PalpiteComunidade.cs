using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class PalpiteComunidade
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Jogo")]
        public int JogoId { get; set; }

        [ForeignKey(nameof(JogoId))]
        public Jogo? Jogo { get; set; }

        [Required]
        [StringLength(450)]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey(nameof(UsuarioId))]
        public IdentityUser? Usuario { get; set; }

        [Required]
        [StringLength(20)]
        public string ResultadoPrevisto { get; set; } = string.Empty;

        [Range(0, 50)]
        public int? GolsCasaPalpite { get; set; }

        [Range(0, 50)]
        public int? GolsVisitantePalpite { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    }
}
