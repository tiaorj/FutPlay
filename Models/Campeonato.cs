using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required(ErrorMessage = "O formato de disputa é obrigatório.")]
        [StringLength(30)]
        [Display(Name = "Formato de Disputa")]
        public string Formato { get; set; } = CampeonatoFormato.PontosCorridos;

        [NotMapped]
        public bool UsaClassificacaoPorGrupos => CampeonatoFormato.UsaGrupos(Formato);

        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime? DataInicio { get; set; }

        [Display(Name = "Data de Fim")]
        [DataType(DataType.Date)]
        public DateTime? DataFim { get; set; }

        public bool Ativo { get; set; } = true;

        public int? ApiLeagueId { get; set; }

        [Display(Name = "FootballDataCompetitionId")]
        public int? FootballDataCompetitionId { get; set; }

        [StringLength(20)]
        [Display(Name = "Código football-data.org")]
        public string? FootballDataCompetitionCode { get; set; }

        [Display(Name = "Temporada football-data.org")]
        public int? FootballDataSeason { get; set; }

        [StringLength(100)]
        public string? Pais { get; set; }

        [StringLength(300)]
        public string? LogoUrl { get; set; }
    }
}
