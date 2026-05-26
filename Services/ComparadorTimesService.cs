using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class ComparadorTimesService
    {
        private readonly AppDbContext _context;

        public ComparadorTimesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ComparadorTimesResultadoViewModel?> CompararAsync(
            int timeAId,
            int timeBId,
            int? campeonatoId)
        {
            var times = await _context.Times
                .AsNoTracking()
                .Where(t => t.Id == timeAId || t.Id == timeBId)
                .ToListAsync();

            var timeA = times.FirstOrDefault(t => t.Id == timeAId);
            var timeB = times.FirstOrDefault(t => t.Id == timeBId);

            if (timeA == null || timeB == null)
            {
                return null;
            }

            var campeonato = campeonatoId.HasValue
                ? await _context.Campeonatos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == campeonatoId.Value)
                : null;

            var jogosQuery = _context.Jogos
                .AsNoTracking()
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.Ativo &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue &&
                    (j.TimeCasaId == timeAId ||
                     j.TimeVisitanteId == timeAId ||
                     j.TimeCasaId == timeBId ||
                     j.TimeVisitanteId == timeBId));

            if (campeonatoId.HasValue)
            {
                jogosQuery = jogosQuery.Where(j => j.CampeonatoId == campeonatoId.Value);
            }

            var jogos = await jogosQuery
                .OrderByDescending(j => j.DataJogo)
                .ToListAsync();

            var classificacoes = campeonatoId.HasValue
                ? await _context.Classificacoes
                    .AsNoTracking()
                    .Include(c => c.Time)
                    .Where(c =>
                        c.Ativo &&
                        c.CampeonatoId == campeonatoId.Value &&
                        (c.TimeId == timeAId || c.TimeId == timeBId))
                    .ToListAsync()
                : new List<Classificacao>();

            var timeAComparado = MontarTimeComparado(
                timeA,
                jogos,
                classificacoes.FirstOrDefault(c => c.TimeId == timeAId));

            var timeBComparado = MontarTimeComparado(
                timeB,
                jogos,
                classificacoes.FirstOrDefault(c => c.TimeId == timeBId));

            return new ComparadorTimesResultadoViewModel
            {
                FonteDados = campeonato != null
                    ? $"Dados de {campeonato.Nome} {campeonato.Ano}"
                    : "Dados gerais dos jogos finalizados",
                TimeA = timeAComparado,
                TimeB = timeBComparado,
                Analise = MontarAnalise(timeAComparado, timeBComparado)
            };
        }

        private static TimeComparadoViewModel MontarTimeComparado(
            Time time,
            List<Jogo> jogos,
            Classificacao? classificacao)
        {
            var ultimos = jogos
                .Where(j => j.TimeCasaId == time.Id || j.TimeVisitanteId == time.Id)
                .OrderByDescending(j => j.DataJogo)
                .Take(5)
                .Select(j => MapearUltimoJogo(j, time.Id))
                .ToList();

            return new TimeComparadoViewModel
            {
                Id = time.Id,
                Nome = time.Nome,
                Sigla = time.Sigla,
                EscudoUrl = time.EscudoUrl,
                Pais = time.Pais,
                Tipo = time.Tipo,
                Classificacao = MapearClassificacao(classificacao, time.Nome),
                Estatisticas = CalcularEstatisticas(jogos, time.Id, time.Nome),
                UltimosJogos = ultimos,
                FormaRecente = CalcularFormaRecente(ultimos)
            };
        }

        private static UltimoJogoTimeViewModel MapearUltimoJogo(Jogo jogo, int timeId)
        {
            var mandante = jogo.TimeCasaId == timeId;
            var golsPro = mandante ? jogo.GolsCasa!.Value : jogo.GolsVisitante!.Value;
            var golsContra = mandante ? jogo.GolsVisitante!.Value : jogo.GolsCasa!.Value;
            var adversario = mandante
                ? jogo.TimeVisitante?.Nome ?? "Visitante"
                : jogo.TimeCasa?.Nome ?? "Mandante";

            return new UltimoJogoTimeViewModel
            {
                JogoId = jogo.Id,
                DataJogo = jogo.DataJogo,
                Campeonato = jogo.Campeonato?.Nome ?? "Campeonato",
                Adversario = adversario,
                Mandante = mandante,
                Placar = $"{jogo.GolsCasa} x {jogo.GolsVisitante}",
                Resultado = golsPro > golsContra ? "V" : golsPro < golsContra ? "D" : "E"
            };
        }

        private static EstatisticasTimePartidaViewModel CalcularEstatisticas(
            List<Jogo> jogos,
            int timeId,
            string nome)
        {
            var estatisticas = new EstatisticasTimePartidaViewModel
            {
                Nome = nome
            };

            foreach (var jogo in jogos.Where(j => j.TimeCasaId == timeId || j.TimeVisitanteId == timeId))
            {
                var mandante = jogo.TimeCasaId == timeId;
                var golsPro = mandante ? jogo.GolsCasa!.Value : jogo.GolsVisitante!.Value;
                var golsContra = mandante ? jogo.GolsVisitante!.Value : jogo.GolsCasa!.Value;

                estatisticas.Jogos++;
                estatisticas.GolsMarcados += golsPro;
                estatisticas.GolsSofridos += golsContra;

                if (golsPro > golsContra)
                {
                    estatisticas.Vitorias++;
                }
                else if (golsPro < golsContra)
                {
                    estatisticas.Derrotas++;
                }
                else
                {
                    estatisticas.Empates++;
                }
            }

            return estatisticas;
        }

        private static ClassificacaoPartidaViewModel? MapearClassificacao(
            Classificacao? classificacao,
            string nomeFallback)
        {
            if (classificacao == null)
            {
                return null;
            }

            return new ClassificacaoPartidaViewModel
            {
                Nome = classificacao.Time?.Nome ?? nomeFallback,
                Posicao = classificacao.Posicao,
                Pontos = classificacao.Pontos,
                Jogos = classificacao.Jogos,
                SaldoGols = classificacao.SaldoGols
            };
        }

        private static double CalcularFormaRecente(List<UltimoJogoTimeViewModel> jogos)
        {
            if (!jogos.Any())
            {
                return 0;
            }

            var pontos = jogos.Sum(j => j.Resultado == "V" ? 3 : j.Resultado == "E" ? 1 : 0);

            return (double)pontos * 100 / (jogos.Count * 3);
        }

        private static AnaliseComparadorTimesViewModel MontarAnalise(
            TimeComparadoViewModel timeA,
            TimeComparadoViewModel timeB)
        {
            var melhorAtaque = CompararMaior(
                timeA.Nome,
                timeB.Nome,
                timeA.Estatisticas.MediaGolsMarcados,
                timeB.Estatisticas.MediaGolsMarcados,
                tolerancia: 0.12);

            var melhorDefesa = CompararMenor(
                timeA.Nome,
                timeB.Nome,
                timeA.Estatisticas.MediaGolsSofridos,
                timeB.Estatisticas.MediaGolsSofridos,
                tolerancia: 0.12);

            var melhorFase = CompararMaior(
                timeA.Nome,
                timeB.Nome,
                timeA.FormaRecente,
                timeB.FormaRecente,
                tolerancia: 8);

            var melhorAproveitamento = CompararMaior(
                timeA.Nome,
                timeB.Nome,
                timeA.Estatisticas.Aproveitamento,
                timeB.Estatisticas.Aproveitamento,
                tolerancia: 8);

            return new AnaliseComparadorTimesViewModel
            {
                MelhorAtaque = melhorAtaque,
                MelhorDefesa = melhorDefesa,
                MelhorFaseRecente = melhorFase,
                MelhorAproveitamento = melhorAproveitamento,
                Tendencia = MontarTendencia(
                    timeA,
                    timeB,
                    melhorDefesa,
                    melhorFase,
                    melhorAproveitamento)
            };
        }

        private static string MontarTendencia(
            TimeComparadoViewModel timeA,
            TimeComparadoViewModel timeB,
            string melhorDefesa,
            string melhorFase,
            string melhorAproveitamento)
        {
            if (melhorFase == timeA.Nome && melhorAproveitamento == timeA.Nome)
            {
                return $"{timeA.Nome} chega em melhor momento recente.";
            }

            if (melhorFase == timeB.Nome && melhorAproveitamento == timeB.Nome)
            {
                return $"{timeB.Nome} chega em melhor momento recente.";
            }

            if (melhorAproveitamento == timeA.Nome && melhorDefesa == timeB.Nome)
            {
                return $"{timeA.Nome} tem melhor aproveitamento, mas {timeB.Nome} sofre menos gols.";
            }

            if (melhorAproveitamento == timeB.Nome && melhorDefesa == timeA.Nome)
            {
                return $"{timeB.Nome} tem melhor aproveitamento, mas {timeA.Nome} sofre menos gols.";
            }

            if (melhorDefesa == timeA.Nome)
            {
                return $"{timeA.Nome} tem melhor defesa nos dados analisados.";
            }

            if (melhorDefesa == timeB.Nome)
            {
                return $"{timeB.Nome} tem melhor defesa nos dados analisados.";
            }

            return "Comparativo equilibrado entre as equipes.";
        }

        private static string CompararMaior(
            string nomeA,
            string nomeB,
            double valorA,
            double valorB,
            double tolerancia)
        {
            if (Math.Abs(valorA - valorB) <= tolerancia)
            {
                return "Equilibrado";
            }

            return valorA > valorB ? nomeA : nomeB;
        }

        private static string CompararMenor(
            string nomeA,
            string nomeB,
            double valorA,
            double valorB,
            double tolerancia)
        {
            if (Math.Abs(valorA - valorB) <= tolerancia)
            {
                return "Equilibrado";
            }

            return valorA < valorB ? nomeA : nomeB;
        }
    }
}
