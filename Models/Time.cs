using System.ComponentModel.DataAnnotations;

namespace FutPlay.Models
{
    public class Time
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do time/seleção é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "A sigla deve ter no máximo 10 caracteres.")]
        public string? Sigla { get; set; }

        [StringLength(100, ErrorMessage = "O país deve ter no máximo 100 caracteres.")]
        public string? Pais { get; set; }

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        [StringLength(30)]
        public string Tipo { get; set; } = string.Empty;

        [Display(Name = "URL do Escudo")]
        [StringLength(300)]
        public string? EscudoUrl { get; set; }

        public bool Ativo { get; set; } = true;

        public int? ApiTeamId { get; set; }
    }
}