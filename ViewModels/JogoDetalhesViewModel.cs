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
}
