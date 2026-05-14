using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCampeonatos { get; set; }
        public int TotalTimes { get; set; }
        public int TotalJogos { get; set; }
        public int TotalJogosAgendados { get; set; }
        public int TotalJogosFinalizados { get; set; }
        public int TotalLigas { get; set; }
        public int TotalParticipantes { get; set; }
        public int TotalPalpites { get; set; }

        public List<Jogo> ProximosJogos { get; set; } = new();

        public List<DashboardRankingViewModel> TopParticipantes { get; set; } = new();
    }

    public class DashboardRankingViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Liga { get; set; } = string.Empty;
        public int Pontos { get; set; }
    }
}