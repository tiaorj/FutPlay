using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class AnalisePartidaService
    {
        private readonly AppDbContext _context;

        public AnalisePartidaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AnalisePartidaResultadoViewModel> AnalisarAsync(Jogo jogo)
        {
            var jogosCampeonato = await ObterJogosFinalizadosCampeonatoAsync(jogo);
            var ultimosCasa = await ObterUltimosJogosAsync(jogo.TimeCasaId, jogo.Id, jogo.DataJogo);
            var ultimosVisitante = await ObterUltimosJogosAsync(jogo.TimeVisitanteId, jogo.Id, jogo.DataJogo);
            var classificacoes = await ObterClassificacoesAsync(jogo);

            var campanhaCasa = CalcularCampanhaTime(
                jogosCampeonato,
                jogo.TimeCasaId,
                jogo.TimeCasa?.Nome ?? "Mandante");

            var campanhaVisitante = CalcularCampanhaTime(
                jogosCampeonato,
                jogo.TimeVisitanteId,
                jogo.TimeVisitante?.Nome ?? "Visitante");

            var comparativo = MontarComparativo(
                jogo,
                jogosCampeonato,
                ultimosCasa,
                ultimosVisitante);

            var classificacaoCasa = MapearClassificacao(
                classificacoes.FirstOrDefault(c => c.TimeId == jogo.TimeCasaId),
                jogo.TimeCasa?.Nome ?? "Mandante");

            var classificacaoVisitante = MapearClassificacao(
                classificacoes.FirstOrDefault(c => c.TimeId == jogo.TimeVisitanteId),
                jogo.TimeVisitante?.Nome ?? "Visitante");

            return new AnalisePartidaResultadoViewModel
            {
                ConfrontosDiretos = await ObterConfrontosDiretosAsync(jogo),
                CampanhaCasa = campanhaCasa,
                CampanhaVisitante = campanhaVisitante,
                UltimosJogosCasa = ultimosCasa,
                UltimosJogosVisitante = ultimosVisitante,
                Comparativo = comparativo,
                ClassificacaoCasa = classificacaoCasa,
                ClassificacaoVisitante = classificacaoVisitante,
                Previsao = MontarPrevisao(
                    comparativo.Casa,
                    comparativo.Visitante,
                    ultimosCasa,
                    ultimosVisitante,
                    classificacaoCasa,
                    classificacaoVisitante)
            };
        }

        private async Task<List<Jogo>> ObterJogosFinalizadosCampeonatoAsync(Jogo jogo)
        {
            return await _context.Jogos
                .AsNoTracking()
                .Where(j =>
                    j.Ativo &&
                    j.Id != jogo.Id &&
                    j.CampeonatoId == jogo.CampeonatoId &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue)
                .ToListAsync();
        }

        private async Task<List<Jogo>> ObterConfrontosDiretosAsync(Jogo jogo)
        {
            return await _context.Jogos
                .AsNoTracking()
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.Ativo &&
                    j.Id != jogo.Id &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue &&
                    ((j.TimeCasaId == jogo.TimeCasaId && j.TimeVisitanteId == jogo.TimeVisitanteId) ||
                     (j.TimeCasaId == jogo.TimeVisitanteId && j.TimeVisitanteId == jogo.TimeCasaId)))
                .OrderByDescending(j => j.DataJogo)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<UltimoJogoTimeViewModel>> ObterUltimosJogosAsync(
            int timeId,
            int jogoAtualId,
            DateTime dataReferencia)
        {
            var jogos = await _context.Jogos
                .AsNoTracking()
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.Ativo &&
                    j.Id != jogoAtualId &&
                    j.DataJogo < dataReferencia &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue &&
                    (j.TimeCasaId == timeId || j.TimeVisitanteId == timeId))
                .OrderByDescending(j => j.DataJogo)
                .Take(5)
                .ToListAsync();

            if (!jogos.Any())
            {
                jogos = await _context.Jogos
                    .AsNoTracking()
                    .Include(j => j.Campeonato)
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j =>
                        j.Ativo &&
                        j.Id != jogoAtualId &&
                        j.Status == "Finalizado" &&
                        j.GolsCasa.HasValue &&
                        j.GolsVisitante.HasValue &&
                        (j.TimeCasaId == timeId || j.TimeVisitanteId == timeId))
                    .OrderByDescending(j => j.DataJogo)
                    .Take(5)
                    .ToListAsync();
            }

            return jogos
                .Select(j => MapearUltimoJogo(j, timeId))
                .ToList();
        }

        private async Task<List<Classificacao>> ObterClassificacoesAsync(Jogo jogo)
        {
            return await _context.Classificacoes
                .AsNoTracking()
                .Include(c => c.Time)
                .Where(c =>
                    c.Ativo &&
                    c.CampeonatoId == jogo.CampeonatoId &&
                    (c.TimeId == jogo.TimeCasaId || c.TimeId == jogo.TimeVisitanteId))
                .ToListAsync();
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

        private static ComparativoPartidaViewModel MontarComparativo(
            Jogo jogo,
            List<Jogo> jogosCampeonato,
            List<UltimoJogoTimeViewModel> ultimosCasa,
            List<UltimoJogoTimeViewModel> ultimosVisitante)
        {
            var jogosCasaCampeonato = jogosCampeonato
                .Where(j => j.TimeCasaId == jogo.TimeCasaId || j.TimeVisitanteId == jogo.TimeCasaId)
                .ToList();

            var jogosVisitanteCampeonato = jogosCampeonato
                .Where(j => j.TimeCasaId == jogo.TimeVisitanteId || j.TimeVisitanteId == jogo.TimeVisitanteId)
                .ToList();

            var usarCampeonato = jogosCasaCampeonato.Any() || jogosVisitanteCampeonato.Any();

            return new ComparativoPartidaViewModel
            {
                FonteDados = usarCampeonato
                    ? "Dados do campeonato atual"
                    : "Últimos jogos disponíveis",
                Casa = usarCampeonato
                    ? CalcularEstatisticas(jogosCasaCampeonato, jogo.TimeCasaId, jogo.TimeCasa?.Nome ?? "Mandante")
                    : CalcularEstatisticas(ultimosCasa, jogo.TimeCasa?.Nome ?? "Mandante"),
                Visitante = usarCampeonato
                    ? CalcularEstatisticas(jogosVisitanteCampeonato, jogo.TimeVisitanteId, jogo.TimeVisitante?.Nome ?? "Visitante")
                    : CalcularEstatisticas(ultimosVisitante, jogo.TimeVisitante?.Nome ?? "Visitante")
            };
        }

        private static CampanhaTimeJogoViewModel CalcularCampanhaTime(
            List<Jogo> jogos,
            int timeId,
            string nome)
        {
            var campanha = new CampanhaTimeJogoViewModel
            {
                Nome = nome
            };

            foreach (var jogo in jogos.Where(j => j.TimeCasaId == timeId || j.TimeVisitanteId == timeId))
            {
                var mandante = jogo.TimeCasaId == timeId;
                var golsPro = mandante ? jogo.GolsCasa!.Value : jogo.GolsVisitante!.Value;
                var golsContra = mandante ? jogo.GolsVisitante!.Value : jogo.GolsCasa!.Value;

                campanha.Jogos++;
                campanha.GolsPro += golsPro;
                campanha.GolsContra += golsContra;

                if (golsPro > golsContra)
                {
                    campanha.Vitorias++;
                }
                else if (golsPro < golsContra)
                {
                    campanha.Derrotas++;
                }
                else
                {
                    campanha.Empates++;
                }
            }

            return campanha;
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

        private static EstatisticasTimePartidaViewModel CalcularEstatisticas(
            List<UltimoJogoTimeViewModel> jogos,
            string nome)
        {
            var estatisticas = new EstatisticasTimePartidaViewModel
            {
                Nome = nome
            };

            foreach (var jogo in jogos)
            {
                var placar = jogo.Placar.Split(" x ");

                if (placar.Length == 2 &&
                    int.TryParse(placar[0], out var golsCasa) &&
                    int.TryParse(placar[1], out var golsVisitante))
                {
                    var golsPro = jogo.Mandante ? golsCasa : golsVisitante;
                    var golsContra = jogo.Mandante ? golsVisitante : golsCasa;

                    estatisticas.Jogos++;
                    estatisticas.GolsMarcados += golsPro;
                    estatisticas.GolsSofridos += golsContra;

                    if (jogo.Resultado == "V")
                    {
                        estatisticas.Vitorias++;
                    }
                    else if (jogo.Resultado == "D")
                    {
                        estatisticas.Derrotas++;
                    }
                    else
                    {
                        estatisticas.Empates++;
                    }
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

        private static PrevisaoPartidaViewModel MontarPrevisao(
            EstatisticasTimePartidaViewModel casa,
            EstatisticasTimePartidaViewModel visitante,
            List<UltimoJogoTimeViewModel> ultimosCasa,
            List<UltimoJogoTimeViewModel> ultimosVisitante,
            ClassificacaoPartidaViewModel? classificacaoCasa,
            ClassificacaoPartidaViewModel? classificacaoVisitante)
        {
            var formaCasa = CalcularFormaRecente(ultimosCasa);
            var formaVisitante = CalcularFormaRecente(ultimosVisitante);
            var mediaCasaPro = casa.Jogos == 0 ? 1.0 : casa.MediaGolsMarcados;
            var mediaCasaContra = casa.Jogos == 0 ? 1.0 : casa.MediaGolsSofridos;
            var mediaVisitantePro = visitante.Jogos == 0 ? 1.0 : visitante.MediaGolsMarcados;
            var mediaVisitanteContra = visitante.Jogos == 0 ? 1.0 : visitante.MediaGolsSofridos;

            var scoreCasa =
                1.0 +
                formaCasa * 0.9 +
                casa.Aproveitamento / 100 * 0.8 +
                mediaCasaPro * 0.22 -
                mediaCasaContra * 0.12 +
                0.22;

            var scoreVisitante =
                1.0 +
                formaVisitante * 0.9 +
                visitante.Aproveitamento / 100 * 0.8 +
                mediaVisitantePro * 0.22 -
                mediaVisitanteContra * 0.12;

            if (classificacaoCasa != null && classificacaoVisitante != null)
            {
                var diferencaPosicao = classificacaoVisitante.Posicao - classificacaoCasa.Posicao;
                var ajusteTabela = Math.Clamp(diferencaPosicao * 0.035, -0.28, 0.28);

                scoreCasa += ajusteTabela;
                scoreVisitante -= ajusteTabela;
            }

            scoreCasa = Math.Max(scoreCasa, 0.25);
            scoreVisitante = Math.Max(scoreVisitante, 0.25);

            var equilibrio = Math.Max(0, 1.0 - Math.Abs(scoreCasa - scoreVisitante));
            var scoreEmpate = 0.72 + equilibrio * 0.35;
            var total = scoreCasa + scoreEmpate + scoreVisitante;

            var probCasa = (int)Math.Round(scoreCasa * 100 / total);
            var probEmpate = (int)Math.Round(scoreEmpate * 100 / total);
            var probVisitante = 100 - probCasa - probEmpate;

            var tendencia = "Jogo equilibrado";

            if (probCasa - probVisitante >= 10)
            {
                tendencia = "Leve vantagem para o mandante";
            }
            else if (probVisitante - probCasa >= 10)
            {
                tendencia = "Visitante chega em melhor momento";
            }
            else if (casa.Jogos + visitante.Jogos == 0 && !ultimosCasa.Any() && !ultimosVisitante.Any())
            {
                tendencia = "Dados ainda limitados para uma leitura forte da partida";
            }

            var golsEsperadosCasa = Math.Clamp((mediaCasaPro + mediaVisitanteContra) / 2 + 0.18, 0.3, 3.2);
            var golsEsperadosVisitante = Math.Clamp((mediaVisitantePro + mediaCasaContra) / 2, 0.2, 3.0);

            return new PrevisaoPartidaViewModel
            {
                ProbabilidadeCasa = probCasa,
                ProbabilidadeEmpate = probEmpate,
                ProbabilidadeVisitante = probVisitante,
                Tendencia = tendencia,
                ResumoAnalise = "Modelo inicial com médias de gols, gols sofridos, aproveitamento recente, mando de campo e posição na tabela quando disponível.",
                PlacaresProvaveis = MontarPlacaresProvaveis(
                    golsEsperadosCasa,
                    golsEsperadosVisitante,
                    probCasa,
                    probEmpate,
                    probVisitante)
            };
        }

        private static double CalcularFormaRecente(List<UltimoJogoTimeViewModel> jogos)
        {
            if (!jogos.Any())
            {
                return 0.45;
            }

            var pontos = jogos.Sum(j => j.Resultado == "V" ? 3 : j.Resultado == "E" ? 1 : 0);

            return (double)pontos / (jogos.Count * 3);
        }

        private static List<string> MontarPlacaresProvaveis(
            double golsEsperadosCasa,
            double golsEsperadosVisitante,
            int probCasa,
            int probEmpate,
            int probVisitante)
        {
            var casaBase = (int)Math.Round(golsEsperadosCasa);
            var visitanteBase = (int)Math.Round(golsEsperadosVisitante);
            var placares = new List<string>
            {
                $"{casaBase} x {visitanteBase}"
            };

            if (probEmpate >= probCasa && probEmpate >= probVisitante)
            {
                placares.Add("1 x 1");
                placares.Add("0 x 0");
            }
            else if (probCasa >= probVisitante)
            {
                placares.Add("1 x 0");
                placares.Add("2 x 1");
            }
            else
            {
                placares.Add("0 x 1");
                placares.Add("1 x 2");
            }

            return placares
                .Distinct()
                .Take(3)
                .ToList();
        }
    }
}
