using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class SimuladorViewModel
    {
        public Campeonato Campeonato { get; set; } = new();

        public List<SimuladorJogoViewModel> Jogos { get; set; } = new();

        public List<SimuladorClassificacaoViewModel> Classificacao { get; set; } = new();

        public bool Simulado { get; set; }

        public bool UsaGrupos => Campeonato.UsaClassificacaoPorGrupos;

        public string FormatoDescricao => CampeonatoFormato.ObterDescricao(Campeonato.Formato);
    }
}
