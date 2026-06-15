namespace FutPlay.ViewModels
{
    public class SimuladorIndexViewModel
    {
        public int Ano { get; set; }

        public List<SimuladorCompeticaoCardViewModel> Competicoes { get; set; } = new();
    }

    public class SimuladorCompeticaoCardViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int Ano { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string Formato { get; set; } = string.Empty;

        public string FormatoDescricao { get; set; } = string.Empty;

        public string Pais { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }
    }
}
