using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class BuscaViewModel
    {
        public string Termo { get; set; } = string.Empty;

        public bool PesquisaExecutada { get; set; }

        public List<Jogo> Jogos { get; set; } = new();

        public List<Liga> Ligas { get; set; } = new();

        public List<Time> Times { get; set; } = new();

        public List<Campeonato> Campeonatos { get; set; } = new();

        public int TotalResultados =>
            Jogos.Count +
            Ligas.Count +
            Times.Count +
            Campeonatos.Count;
    }
}
