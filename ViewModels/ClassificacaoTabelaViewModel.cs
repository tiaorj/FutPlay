using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class ClassificacaoTabelaViewModel
    {
        public List<Classificacao> Classificacoes { get; set; } = new();

        public List<Grupo> Grupos { get; set; } = new();

        public List<CampeonatoTime> CampeonatoTimes { get; set; } = new();

        public Dictionary<int, string> GruposPorTimeId { get; set; } = new();

        public Dictionary<int, List<string>> UltimosResultadosPorTime { get; set; } = new();

        public string EmptyMessage { get; set; } = "Nenhuma classificacao disponivel.";

        public bool AgruparPorGrupo { get; set; } = true;
    }
}
