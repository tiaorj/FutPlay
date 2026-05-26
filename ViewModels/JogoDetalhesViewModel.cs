using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class JogoDetalhesViewModel
    {
        public Jogo Jogo { get; set; } = new();

        public int TotalPalpites { get; set; }

        public int PalpitesVitoriaCasa { get; set; }

        public int PalpitesEmpate { get; set; }

        public int PalpitesVitoriaVisitante { get; set; }

        public int PalpitesComPontos { get; set; }

        public int PalpitesPlacarExato { get; set; }

        public int TotalPontosDistribuidos { get; set; }

        public double MediaPontos => TotalPalpites == 0
            ? 0
            : (double)TotalPontosDistribuidos / TotalPalpites;

        public List<Jogo> ConfrontosDiretos { get; set; } = new();

        public CampanhaTimeJogoViewModel CampanhaCasa { get; set; } = new();

        public CampanhaTimeJogoViewModel CampanhaVisitante { get; set; } = new();

        public List<UltimoJogoTimeViewModel> UltimosJogosCasa { get; set; } = new();

        public List<UltimoJogoTimeViewModel> UltimosJogosVisitante { get; set; } = new();

        public ComparativoPartidaViewModel Comparativo { get; set; } = new();

        public ClassificacaoPartidaViewModel? ClassificacaoCasa { get; set; }

        public ClassificacaoPartidaViewModel? ClassificacaoVisitante { get; set; }

        public PrevisaoPartidaViewModel Previsao { get; set; } = new();

        public PalpiteComunidadeResumoViewModel PalpiteComunidade { get; set; } = new();
    }

    public class CampanhaTimeJogoViewModel
    {
        public string Nome { get; set; } = string.Empty;

        public int Jogos { get; set; }

        public int Vitorias { get; set; }

        public int Empates { get; set; }

        public int Derrotas { get; set; }

        public int GolsPro { get; set; }

        public int GolsContra { get; set; }

        public int Pontos => Vitorias * 3 + Empates;

        public int Saldo => GolsPro - GolsContra;

        public double Aproveitamento => Jogos == 0
            ? 0
            : (double)Pontos * 100 / (Jogos * 3);
    }

    public class UltimoJogoTimeViewModel
    {
        public int JogoId { get; set; }

        public DateTime DataJogo { get; set; }

        public string Campeonato { get; set; } = string.Empty;

        public string Adversario { get; set; } = string.Empty;

        public string Placar { get; set; } = string.Empty;

        public string Resultado { get; set; } = string.Empty;

        public bool Mandante { get; set; }

        public string BadgeClasse => Resultado switch
        {
            "V" => "win",
            "D" => "loss",
            _ => "draw"
        };
    }

    public class ComparativoPartidaViewModel
    {
        public string FonteDados { get; set; } = "Dados locais";

        public EstatisticasTimePartidaViewModel Casa { get; set; } = new();

        public EstatisticasTimePartidaViewModel Visitante { get; set; } = new();
    }

    public class EstatisticasTimePartidaViewModel
    {
        public string Nome { get; set; } = string.Empty;

        public int Jogos { get; set; }

        public int Vitorias { get; set; }

        public int Empates { get; set; }

        public int Derrotas { get; set; }

        public int GolsMarcados { get; set; }

        public int GolsSofridos { get; set; }

        public int SaldoGols => GolsMarcados - GolsSofridos;

        public int Pontos => Vitorias * 3 + Empates;

        public double Aproveitamento => Jogos == 0
            ? 0
            : (double)Pontos * 100 / (Jogos * 3);

        public double MediaGolsMarcados => Jogos == 0
            ? 0
            : (double)GolsMarcados / Jogos;

        public double MediaGolsSofridos => Jogos == 0
            ? 0
            : (double)GolsSofridos / Jogos;
    }

    public class ClassificacaoPartidaViewModel
    {
        public string Nome { get; set; } = string.Empty;

        public int Posicao { get; set; }

        public int Pontos { get; set; }

        public int Jogos { get; set; }

        public int SaldoGols { get; set; }
    }

    public class PrevisaoPartidaViewModel
    {
        public int ProbabilidadeCasa { get; set; }

        public int ProbabilidadeEmpate { get; set; }

        public int ProbabilidadeVisitante { get; set; }

        public string Tendencia { get; set; } = "Jogo equilibrado";

        public string ResumoAnalise { get; set; } = string.Empty;

        public List<string> PlacaresProvaveis { get; set; } = new();
    }

    public class AnalisePartidaResultadoViewModel
    {
        public List<Jogo> ConfrontosDiretos { get; set; } = new();

        public CampanhaTimeJogoViewModel CampanhaCasa { get; set; } = new();

        public CampanhaTimeJogoViewModel CampanhaVisitante { get; set; } = new();

        public List<UltimoJogoTimeViewModel> UltimosJogosCasa { get; set; } = new();

        public List<UltimoJogoTimeViewModel> UltimosJogosVisitante { get; set; } = new();

        public ComparativoPartidaViewModel Comparativo { get; set; } = new();

        public ClassificacaoPartidaViewModel? ClassificacaoCasa { get; set; }

        public ClassificacaoPartidaViewModel? ClassificacaoVisitante { get; set; }

        public PrevisaoPartidaViewModel Previsao { get; set; } = new();
    }
}
