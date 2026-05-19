using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class ClassificacaoCampeonatoViewModel
    {
        public Campeonato Campeonato { get; set; } = new Campeonato();

        public List<Classificacao> Classificacoes { get; set; } = new();

        public Dictionary<int, List<string>> UltimosResultadosPorTime { get; set; } = new();

        public bool ExibirPorGrupos => Campeonato.UsaClassificacaoPorGrupos;

        public string FormatoDescricao => CampeonatoFormato.ObterDescricao(Campeonato.Formato);
    }
}
