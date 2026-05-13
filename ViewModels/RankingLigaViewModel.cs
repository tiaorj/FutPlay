using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class RankingLigaViewModel
    {
        public Liga Liga { get; set; } = new Liga();

        public List<RankingParticipanteViewModel> Participantes { get; set; } = new();
    }

    public class RankingParticipanteViewModel
    {
        public int Posicao { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int PontuacaoTotal { get; set; }

        public int TotalPalpites { get; set; }

        public int PlacaresExatos { get; set; }
    }
}