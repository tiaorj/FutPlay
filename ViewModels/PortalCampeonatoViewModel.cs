using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class PortalCampeonatoViewModel
    {
        public Campeonato Campeonato { get; set; } = new Campeonato();

        public List<Classificacao> Classificacoes { get; set; } = new();

        public List<Jogo> JogosDaRodada { get; set; } = new();

        public List<Jogo> ProximosJogos { get; set; } = new();

        public List<Jogo> JogosFinalizados { get; set; } = new();

        public List<RodadaFiltroViewModel> Rodadas { get; set; } = new();

        public Dictionary<int, List<string>> UltimosResultadosPorTime { get; set; } = new();

        public string Aba { get; set; } = "visao-geral";

        public int? RodadaSelecionada { get; set; }

        public int? RodadaAnterior { get; set; }

        public int? ProximaRodada { get; set; }

        public int TotalJogos { get; set; }

        public int TotalHoje { get; set; }

        public int TotalProximos { get; set; }

        public int TotalFinalizados { get; set; }

        public bool AbaVisaoGeralAtiva => Aba == "visao-geral";

        public bool AbaJogosAtiva => Aba == "jogos";

        public bool AbaClassificacaoAtiva => Aba == "classificacao";
    }
}
