using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class ClassificacaoCampeonatoViewModel
    {
        public Campeonato Campeonato { get; set; } = new Campeonato();

        public List<Classificacao> Classificacoes { get; set; } = new();
    }
}