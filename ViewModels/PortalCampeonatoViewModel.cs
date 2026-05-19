using FutPlay.Models;
using System.Collections.Generic;

namespace FutPlay.ViewModels
{
    public class PortalCampeonatoViewModel
    {
        public Campeonato Campeonato { get; set; } = new Campeonato();

        public List<Classificacao> Classificacoes { get; set; } = new();

        public List<Jogo> JogosDaRodada { get; set; } = new();

        public List<Jogo> Jogos { get; set; } = new();

        public List<Jogo> ProximosJogos { get; set; } = new();

        public List<Jogo> JogosFinalizados { get; set; } = new();

        public List<RodadaFiltroViewModel> Rodadas { get; set; } = new();

        // usa o DataFiltroViewModel definido em ViewModels/DataFiltroViewModel.cs
        public List<DataFiltroViewModel> Datas { get; set; } = new();
        public string? Modo { get; set; } = "rodada";
        public string? DataSelecionada { get; set; }

        public Dictionary<int, List<string>> UltimosResultadosPorTime { get; set; } = new();

        public string Aba { get; set; } = "visao-geral";

        public int? RodadaSelecionada { get; set; }

        public int? RodadaAnterior { get; set; }

        public int? ProximaRodada { get; set; }

        public int TotalJogos { get; set; }

        public int TotalHoje { get; set; }

        public int TotalProximos { get; set; }

        public int TotalFinalizados { get; set; }

        public bool ExibirClassificacaoPorGrupos => Campeonato.UsaClassificacaoPorGrupos;

        public string FormatoDescricao => CampeonatoFormato.ObterDescricao(Campeonato.Formato);

        public bool AbaVisaoGeralAtiva => Aba == "visao-geral";

        public bool AbaJogosAtiva => Aba == "jogos";

        public bool AbaClassificacaoAtiva => Aba == "classificacao";

        public bool AbaFaseEliminatoriaAtiva => Aba == "fase-eliminatoria";

        public bool AbaEstatisticasAtiva => Aba == "estatisticas";

        public bool AbaMidiaAtiva => Aba == "midia";
    }
}
