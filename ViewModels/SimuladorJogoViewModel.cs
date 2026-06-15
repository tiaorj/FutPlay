namespace FutPlay.ViewModels
{
    public class SimuladorJogoViewModel
    {
        public int JogoId { get; set; }

        public int TimeCasaId { get; set; }

        public string TimeCasaNome { get; set; } = string.Empty;

        public string? TimeCasaEscudoUrl { get; set; }

        public string TimeCasaSigla { get; set; } = "CAS";

        public int TimeVisitanteId { get; set; }

        public string TimeVisitanteNome { get; set; } = string.Empty;

        public string? TimeVisitanteEscudoUrl { get; set; }

        public string TimeVisitanteSigla { get; set; } = "VIS";

        public DateTime DataJogo { get; set; }

        public int? Rodada { get; set; }

        public string Grupo { get; set; } = "Sem grupo";

        public string GrupoChave { get; set; } = string.Empty;

        public string Fase { get; set; } = "Fase a definir";

        public int OrdemFase { get; set; }

        public int? PlacarCasa { get; set; }

        public int? PlacarVisitante { get; set; }

        public bool TemPlacar => PlacarCasa.HasValue && PlacarVisitante.HasValue;
    }
}
