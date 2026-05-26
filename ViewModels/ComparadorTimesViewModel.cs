using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class ComparadorTimesViewModel
    {
        public int? TimeAId { get; set; }

        public int? TimeBId { get; set; }

        public int? CampeonatoId { get; set; }

        public List<Time> Times { get; set; } = new();

        public List<Campeonato> Campeonatos { get; set; } = new();

        public ComparadorTimesResultadoViewModel? Resultado { get; set; }

        public string? Mensagem { get; set; }
    }

    public class ComparadorTimesResultadoViewModel
    {
        public string FonteDados { get; set; } = "Dados gerais";

        public TimeComparadoViewModel TimeA { get; set; } = new();

        public TimeComparadoViewModel TimeB { get; set; } = new();

        public AnaliseComparadorTimesViewModel Analise { get; set; } = new();
    }

    public class TimeComparadoViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Sigla { get; set; }

        public string? EscudoUrl { get; set; }

        public string? Pais { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public ClassificacaoPartidaViewModel? Classificacao { get; set; }

        public EstatisticasTimePartidaViewModel Estatisticas { get; set; } = new();

        public List<UltimoJogoTimeViewModel> UltimosJogos { get; set; } = new();

        public double FormaRecente { get; set; }
    }

    public class AnaliseComparadorTimesViewModel
    {
        public string Tendencia { get; set; } = "Comparativo equilibrado entre as equipes.";

        public string MelhorAtaque { get; set; } = "Equilibrado";

        public string MelhorDefesa { get; set; } = "Equilibrado";

        public string MelhorFaseRecente { get; set; } = "Equilibrado";

        public string MelhorAproveitamento { get; set; } = "Equilibrado";
    }
}
