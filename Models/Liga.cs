using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class Liga
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da liga é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campeonato é obrigatório.")]
        [Display(Name = "Campeonato")]
        public int CampeonatoId { get; set; }

        [ForeignKey("CampeonatoId")]
        public Campeonato? Campeonato { get; set; }

        [Display(Name = "Código de Convite")]
        [StringLength(20)]
        public string CodigoConvite { get; set; } = string.Empty;

        [Display(Name = "Liga Pública")]
        public bool Publica { get; set; } = false;

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public bool Ativo { get; set; } = true;
    }
}