using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class JogosIndexViewModel
    {
        public List<Jogo> Jogos { get; set; } = new();
        public List<RodadaFiltroViewModel> Rodadas { get; set; } = new();
        public List<CampeonatoFiltroViewModel> Campeonatos { get; set; } = new();
        public HashSet<int> TimesFavoritosIds { get; set; } = new();

        public string Aba { get; set; } = "todos";
        public string Filtro { get; set; } = "todos";
        public int? RodadaSelecionada { get; set; }
        public int? RodadaAnterior { get; set; }
        public int? ProximaRodada { get; set; }
        public int? CampeonatoId { get; set; }

        public bool UsuarioAutenticado { get; set; }
        public int TotalJogos { get; set; }
        public int TotalHoje { get; set; }
        public int TotalProximos { get; set; }
        public int TotalFinalizados { get; set; }
        public int TotalFavoritos { get; set; }
        public int TotalTimesFavoritos { get; set; }

        public bool AbaTodosAtiva => Aba == "todos";
        public bool AbaFavoritosAtiva => Aba == "favoritos";
        public bool AbaCompeticoesAtiva => Aba == "competicoes";
        public bool AgruparPorCampeonato => AbaCompeticoesAtiva || CampeonatoId.HasValue;
    }

    public class RodadaFiltroViewModel
    {
        public int Rodada { get; set; }
        public DateTime DataReferencia { get; set; }
        public int TotalJogos { get; set; }
        public bool Selecionada { get; set; }
    }

    public class CampeonatoFiltroViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Pais { get; set; }
        public string? Tipo { get; set; }
        public string? LogoUrl { get; set; }
        public int Ano { get; set; }
        public int TotalJogos { get; set; }
        public bool Selecionado { get; set; }
    }
}
