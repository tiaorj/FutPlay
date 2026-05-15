using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class PortalCampeonatoViewModel
    {
        public Campeonato Campeonato { get; set; } = new Campeonato();

        public List<Classificacao> Classificacoes { get; set; } = new();

        public List<Jogo> ProximosJogos { get; set; } = new();

        public List<Jogo> JogosFinalizados { get; set; } = new();

        public Dictionary<int, List<string>> UltimosResultadosPorTime { get; set; } = new();
    }
}
