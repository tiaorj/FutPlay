namespace FutPlay.ViewModels
{
    public class MinhaLigaViewModel
    {
        public int LigaId { get; set; }

        public int LigaParticipanteId { get; set; }

        public string NomeLiga { get; set; } = string.Empty;

        public string NomeCampeonato { get; set; } = string.Empty;

        public int Pontuacao { get; set; }
    }
}
