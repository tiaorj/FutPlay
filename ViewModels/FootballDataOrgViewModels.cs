namespace FutPlay.ViewModels
{
    public class FootballDataOrgCompeticaoViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Codigo { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public string Plano { get; set; } = string.Empty;

        public string? Pais { get; set; }

        public string? EmblemaUrl { get; set; }

        public int? TemporadaAtual { get; set; }

        public DateTime? TemporadaAtualInicio { get; set; }

        public DateTime? TemporadaAtualFim { get; set; }

        public int? RodadaAtual { get; set; }
    }
}
