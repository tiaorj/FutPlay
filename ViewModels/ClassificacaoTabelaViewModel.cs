using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class ClassificacaoTabelaViewModel
    {
        public List<Classificacao> Classificacoes { get; set; } = new();

        public Dictionary<int, List<string>> UltimosResultadosPorTime { get; set; } = new();

        public string EmptyMessage { get; set; } = "Nenhuma classificacao disponivel.";
    }
}
