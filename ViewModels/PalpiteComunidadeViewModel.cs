namespace FutPlay.ViewModels
{
    public class PalpiteComunidadeViewModel
    {
        public int JogoId { get; set; }

        public string ResultadoPrevisto { get; set; } = string.Empty;

        public int? GolsCasaPalpite { get; set; }

        public int? GolsVisitantePalpite { get; set; }
    }

    public class PalpiteComunidadeResumoViewModel
    {
        public int JogoId { get; set; }

        public int TotalPalpites { get; set; }

        public int TotalCasa { get; set; }

        public int TotalEmpate { get; set; }

        public int TotalVisitante { get; set; }

        public int PercentualCasa { get; set; }

        public int PercentualEmpate { get; set; }

        public int PercentualVisitante { get; set; }

        public string? PlacarMaisEscolhido { get; set; }

        public int TotalPlacarMaisEscolhido { get; set; }

        public List<PalpiteComunidadePlacarViewModel> PlacaresMaisEscolhidos { get; set; } = new();

        public PalpiteComunidadeUsuarioViewModel? PalpiteUsuario { get; set; }

        public bool Bloqueado { get; set; }

        public DateTime DataBloqueio { get; set; }

        public string MensagemBloqueio { get; set; } = string.Empty;

        public bool UsuarioPodePalpitar { get; set; }

        public bool JogoFinalizado { get; set; }

        public string? ResultadoReal { get; set; }

        public bool? UsuarioAcertouResultado { get; set; }

        public bool? UsuarioAcertouPlacarExato { get; set; }
    }

    public class PalpiteComunidadeUsuarioViewModel
    {
        public string ResultadoPrevisto { get; set; } = string.Empty;

        public int? GolsCasaPalpite { get; set; }

        public int? GolsVisitantePalpite { get; set; }

        public DateTime AtualizadoEm { get; set; }

        public string ResultadoTexto { get; set; } = string.Empty;

        public string? PlacarTexto { get; set; }
    }

    public class PalpiteComunidadePlacarViewModel
    {
        public string Placar { get; set; } = string.Empty;

        public int Total { get; set; }

        public int Percentual { get; set; }
    }

    public class PalpiteComunidadeSalvarResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;
    }
}
