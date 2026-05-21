namespace FutPlay.ViewModels
{
    public class ApiTimeViewModel
    {
        public int ApiTeamId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Pais { get; set; }

        public string? CodigoPais { get; set; }

        public string? Sigla { get; set; }

        public string? Tipo { get; set; }

        public string? EscudoUrl { get; set; }

        public bool JaImportado { get; set; }

        public int? TimeExistenteId { get; set; }
    }
}
