using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class TimesIndexViewModel
    {
        public List<Time> Times { get; set; } = new();
        public List<string> Paises { get; set; } = new();
        public HashSet<int> TimesFavoritosIds { get; set; } = new();

        public string Filtro { get; set; } = "todos";
        public string? Pais { get; set; }
        public bool UsuarioAutenticado { get; set; }

        public int TotalTimes { get; set; }
        public int TotalAtivos { get; set; }
        public int TotalClubes { get; set; }
        public int TotalSelecoes { get; set; }
        public int TotalFavoritos { get; set; }
    }
}
