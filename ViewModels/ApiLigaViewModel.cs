namespace FutPlay.ViewModels
{
    public class ApiLigaViewModel
    {
        public int ApiLeagueId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Pais { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public int Temporada { get; set; }

        public string? LogoUrl { get; set; }

        public bool JaImportado { get; set; }

        public int? CampeonatoExistenteId { get; set; }
    }
}
