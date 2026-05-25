using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class TimeDetalhesViewModel
    {
        public Time Time { get; set; } = new();

        public List<Jogo> ProximosJogos { get; set; } = new();

        public List<Jogo> UltimosResultados { get; set; } = new();

        public List<Campeonato> Campeonatos { get; set; } = new();

        public int TotalJogos { get; set; }

        public int TotalFinalizados { get; set; }

        public int TotalVitorias { get; set; }

        public int TotalEmpates { get; set; }

        public int TotalDerrotas { get; set; }

        public int GolsPro { get; set; }

        public int GolsContra { get; set; }

        public int SaldoGols => GolsPro - GolsContra;

        public double Aproveitamento => TotalFinalizados == 0
            ? 0
            : (double)(TotalVitorias * 3 + TotalEmpates) * 100 / (TotalFinalizados * 3);

        public double MediaGolsPro => TotalFinalizados == 0
            ? 0
            : (double)GolsPro / TotalFinalizados;

        public double MediaGolsContra => TotalFinalizados == 0
            ? 0
            : (double)GolsContra / TotalFinalizados;

        public Jogo? ProximoJogo { get; set; }

        public Jogo? UltimoResultado { get; set; }

        public string HistoricoResumo { get; set; } = string.Empty;
    }
}
