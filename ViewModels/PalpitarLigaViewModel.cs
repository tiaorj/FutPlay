using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FutPlay.ViewModels
{
    public class PalpitarLigaViewModel
    {
        public int LigaId { get; set; }

        public string NomeLiga { get; set; } = string.Empty;

        public string NomeCampeonato { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o participante.")]
        [Display(Name = "Participante")]
        public int LigaParticipanteId { get; set; }

        public List<SelectListItem> Participantes { get; set; } = new();

        public List<PalpiteJogoViewModel> Jogos { get; set; } = new();

        public List<RodadaFiltroViewModel> Rodadas { get; set; } = new();

        public int? RodadaSelecionada { get; set; }

        public int? RodadaAnterior { get; set; }

        public int? ProximaRodada { get; set; }

        public int TotalJogosCampeonato { get; set; }

        public bool ParticipanteBloqueado { get; set; }

        public string? NomeParticipanteSelecionado { get; set; }

        public string? Origem { get; set; }
    }

    public class PalpiteJogoViewModel
    {
        public int JogoId { get; set; }

        public string TimeCasa { get; set; } = string.Empty;

        public string? TimeCasaSigla { get; set; }

        public string? TimeCasaEscudoUrl { get; set; }

        public string TimeVisitante { get; set; } = string.Empty;

        public string? TimeVisitanteSigla { get; set; }

        public string? TimeVisitanteEscudoUrl { get; set; }

        public DateTime DataJogo { get; set; }

        public string? Fase { get; set; }

        public string? Grupo { get; set; }

        public int? Rodada { get; set; }

        [Range(0, 50, ErrorMessage = "Informe um valor entre 0 e 50.")]
        public int? GolsCasaPalpite { get; set; }

        [Range(0, 50, ErrorMessage = "Informe um valor entre 0 e 50.")]
        public int? GolsVisitantePalpite { get; set; }

        public bool JaPalpitado { get; set; }

        public bool Bloqueado { get; set; }
    }
}
