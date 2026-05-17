namespace FutPlay.ViewModels
{
    public class MinhaLigaViewModel
    {
        public int LigaId { get; set; }

        public int LigaParticipanteId { get; set; }

        public string NomeLiga { get; set; } = string.Empty;

        public string NomeCampeonato { get; set; } = string.Empty;

        public string? CampeonatoLogoUrl { get; set; }

        public int Pontuacao { get; set; }

        public bool PodeGerenciar { get; set; }

        public bool Publica { get; set; }

        public bool CriadorDaLiga { get; set; }
    }
}
