using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class RadarRodadaViewModel
    {
        public int? CampeonatoId { get; set; }

        public int? Rodada { get; set; }

        public DateTime? Data { get; set; }

        public string Periodo { get; set; } = string.Empty;

        public string PeriodoResolvido { get; set; } = string.Empty;

        public string TituloContexto { get; set; } = "Radar da Rodada";

        public List<Campeonato> Campeonatos { get; set; } = new();

        public List<int> Rodadas { get; set; } = new();

        public List<RadarJogoItemViewModel> Jogos { get; set; } = new();

        public List<RadarJogoItemViewModel> JogosHoje { get; set; } = new();

        public List<RadarJogoItemViewModel> ProximosJogos { get; set; } = new();

        public List<RadarJogoItemViewModel> JogosEquilibrados { get; set; } = new();

        public List<RadarJogoItemViewModel> FavoritosRodada { get; set; } = new();

        public List<RadarJogoItemViewModel> TendenciaGols { get; set; } = new();

        public List<RadarJogoItemViewModel> JogosDecisivos { get; set; } = new();

        public List<RadarTimeMomentoViewModel> TimesEmAlta { get; set; } = new();

        public bool TemFiltros => CampeonatoId.HasValue ||
            Rodada.HasValue ||
            Data.HasValue ||
            !string.IsNullOrWhiteSpace(Periodo);
    }

    public class RadarJogoItemViewModel
    {
        public int JogoId { get; set; }

        public string Campeonato { get; set; } = string.Empty;

        public int? CampeonatoId { get; set; }

        public int? Rodada { get; set; }

        public string? Fase { get; set; }

        public string? Grupo { get; set; }

        public DateTime DataJogo { get; set; }

        public string TimeCasa { get; set; } = string.Empty;

        public int TimeCasaId { get; set; }

        public string TimeVisitante { get; set; } = string.Empty;

        public int TimeVisitanteId { get; set; }

        public string? EscudoCasa { get; set; }

        public string? EscudoVisitante { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Placar { get; set; } = "x";

        public int ProbabilidadeCasa { get; set; }

        public int ProbabilidadeEmpate { get; set; }

        public int ProbabilidadeVisitante { get; set; }

        public string Tendencia { get; set; } = string.Empty;

        public int IndicadorEquilibrio { get; set; }

        public string IndicadorGols { get; set; } = string.Empty;

        public string TextoResumo { get; set; } = string.Empty;

        public string BadgePrincipal { get; set; } = string.Empty;

        public bool Equilibrado { get; set; }

        public bool FavoritoClaro { get; set; }

        public bool TendenciaDeGols { get; set; }

        public bool Decisivo { get; set; }

        public string MotivoDecisivo { get; set; } = string.Empty;

        public double MediaGolsProjetada { get; set; }

        public double AproveitamentoCasa { get; set; }

        public double AproveitamentoVisitante { get; set; }

        public int SaldoRecenteCasa { get; set; }

        public int SaldoRecenteVisitante { get; set; }

        public int SequenciaSemPerderCasa { get; set; }

        public int SequenciaSemPerderVisitante { get; set; }

        public List<string> FormaCasa { get; set; } = new();

        public List<string> FormaVisitante { get; set; } = new();

        public int? PosicaoCasa { get; set; }

        public int? PosicaoVisitante { get; set; }

        public int? PontosCasa { get; set; }

        public int? PontosVisitante { get; set; }
    }

    public class RadarTimeMomentoViewModel
    {
        public int TimeId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? EscudoUrl { get; set; }

        public double AproveitamentoRecente { get; set; }

        public int SaldoRecente { get; set; }

        public int SequenciaSemPerder { get; set; }

        public string ProximoAdversario { get; set; } = string.Empty;

        public int JogoId { get; set; }

        public List<string> Forma { get; set; } = new();
    }
}
