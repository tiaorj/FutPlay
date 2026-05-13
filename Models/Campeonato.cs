using System.ComponentModel.DataAnnotations;

namespace FutPlay.Models
{
    public class Campeonato
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do campeonato é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O ano é obrigatório.")]
        public int Ano { get; set; }

        [Required(ErrorMessage = "O tipo do campeonato é obrigatório.")]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime? DataInicio { get; set; }

        [Display(Name = "Data de Fim")]
        [DataType(DataType.Date)]
        public DateTime? DataFim { get; set; }

        public bool Ativo { get; set; } = true;
    }
}