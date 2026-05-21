using System;
using System.Collections.Generic;

namespace FutPlay.ViewModels
{
    public class ChaveamentoFaseViewModel
    {
        public string Nome { get; set; } = string.Empty;

        public int Ordem { get; set; }

        public bool Selecionada { get; set; }

        public string PosicaoCss { get; set; } = string.Empty;

        public List<ChaveamentoSerieViewModel> Series { get; set; } = new();
    }

    public class ChaveamentoSerieViewModel
    {
        public string Fase { get; set; } = string.Empty;

        public int TimeAId { get; set; }

        public string TimeANome { get; set; } = string.Empty;

        public string? TimeAEscudoUrl { get; set; }

        public string TimeASigla { get; set; } = string.Empty;

        public int TimeBId { get; set; }

        public string TimeBNome { get; set; } = string.Empty;

        public string? TimeBEscudoUrl { get; set; }

        public string TimeBSigla { get; set; } = string.Empty;

        public int? AgregadoTimeA { get; set; }

        public int? AgregadoTimeB { get; set; }

        public int? ClassificadoTimeId { get; set; }

        public string StatusSerie { get; set; } = "Série indefinida";

        public List<ChaveamentoJogoViewModel> Jogos { get; set; } = new();

        public bool TemIdaVolta => Jogos.Count > 1;
    }

    public class ChaveamentoJogoViewModel
    {
        public int JogoId { get; set; }

        public DateTime DataJogo { get; set; }

        public string Ordem { get; set; } = string.Empty;

        public int MandanteId { get; set; }

        public string MandanteNome { get; set; } = string.Empty;

        public int VisitanteId { get; set; }

        public string VisitanteNome { get; set; } = string.Empty;

        public int? GolsMandante { get; set; }

        public int? GolsVisitante { get; set; }

        public int? GolsTimeA { get; set; }

        public int? GolsTimeB { get; set; }
    }
}
