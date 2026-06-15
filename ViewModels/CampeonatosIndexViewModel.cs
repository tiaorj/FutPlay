using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class CampeonatosIndexViewModel
    {
        public List<Campeonato> Campeonatos { get; set; } = new();
        public List<string> Paises { get; set; } = new();
        public List<string> Tipos { get; set; } = new();
        public List<int> Anos { get; set; } = new();
        public HashSet<int> CampeonatosFavoritosIds { get; set; } = new();

        public string Filtro { get; set; } = "todos";
        public string? Pais { get; set; }
        public string? Tipo { get; set; }
        public int Ano { get; set; }
        public bool UsuarioAutenticado { get; set; }

        public int TotalCampeonatos { get; set; }
        public int TotalAtivos { get; set; }
        public int TotalInativos { get; set; }
        public int TotalPaises { get; set; }
        public int TotalFavoritos { get; set; }
    }
}
