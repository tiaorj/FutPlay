namespace FutPlay.ViewModels
{
    public class SimuladorClassificacaoViewModel
    {
        public int TimeId { get; set; }

        public string TimeNome { get; set; } = string.Empty;

        public string? EscudoUrl { get; set; }

        public string Sigla { get; set; } = "TIM";

        public string Grupo { get; set; } = "Geral";

        public string GrupoChave { get; set; } = string.Empty;

        public int Posicao { get; set; }

        public int Jogos { get; set; }

        public int Vitorias { get; set; }

        public int Empates { get; set; }

        public int Derrotas { get; set; }

        public int GolsPro { get; set; }

        public int GolsContra { get; set; }

        public int SaldoGols => GolsPro - GolsContra;

        public int Pontos { get; set; }
    }
}
