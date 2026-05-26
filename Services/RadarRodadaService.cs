using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class RadarRodadaService
    {
        private const string PeriodoHoje = "hoje";
        private const string PeriodoProximos = "proximos";
        private const string PeriodoFinalizados = "finalizados";
        private const string PeriodoRodadaAtual = "rodada-atual";

        private readonly AppDbContext _context;
        private readonly AnalisePartidaService _analisePartidaService;

        public RadarRodadaService(
            AppDbContext context,
            AnalisePartidaService analisePartidaService)
        {
            _context = context;
            _analisePartidaService = analisePartidaService;
        }

        public async Task<RadarRodadaViewModel> MontarAsync(
            int? campeonatoId,
            int? rodada,
            DateTime? data,
            string? periodo)
        {
            var hoje = DateTime.Today;
            var periodoNormalizado = NormalizarPeriodo(periodo);
            var somenteCampeonatoSelecionado = campeonatoId.HasValue &&
                !rodada.HasValue &&
                !data.HasValue &&
                string.IsNullOrWhiteSpace(periodoNormalizado);

            var campeonatos = await _context.Campeonatos
                .AsNoTracking()
                .Where(c => c.Ativo)
                .OrderByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            var rodadas = await ObterRodadasAsync(campeonatoId);
            var jogos = await ObterJogosFiltradosAsync(
                campeonatoId,
                rodada,
                data,
                periodoNormalizado,
                hoje);

            var itens = new List<RadarJogoItemViewModel>();

            foreach (var jogo in jogos)
            {
                var analise = await _analisePartidaService.AnalisarAsync(jogo);
                itens.Add(MapearJogo(jogo, analise));
            }

            var viewModel = new RadarRodadaViewModel
            {
                CampeonatoId = campeonatoId,
                Rodada = rodada,
                Data = data,
                Periodo = periodoNormalizado,
                PeriodoResolvido = ResolverPeriodo(periodoNormalizado, somenteCampeonatoSelecionado, data, rodada, campeonatoId, itens),
                TituloContexto = MontarTituloContexto(campeonatos, campeonatoId, rodada, data, periodoNormalizado, itens),
                Campeonatos = campeonatos,
                Rodadas = rodadas,
                Jogos = itens,
                JogosHoje = itens
                    .Where(j => j.DataJogo.Date == hoje)
                    .OrderBy(j => j.DataJogo)
                    .Take(8)
                    .ToList(),
                ProximosJogos = itens
                    .Where(j => j.DataJogo.Date >= hoje && !EhFinalizado(j.Status))
                    .OrderBy(j => j.DataJogo)
                    .Take(8)
                    .ToList(),
                JogosEquilibrados = itens
                    .Where(j => j.Equilibrado)
                    .OrderByDescending(j => j.IndicadorEquilibrio)
                    .Take(6)
                    .ToList(),
                FavoritosRodada = itens
                    .Where(j => j.FavoritoClaro)
                    .OrderByDescending(j => Math.Max(j.ProbabilidadeCasa, j.ProbabilidadeVisitante))
                    .Take(6)
                    .ToList(),
                TendenciaGols = itens
                    .Where(j => j.TendenciaDeGols)
                    .OrderByDescending(j => j.MediaGolsProjetada)
                    .Take(6)
                    .ToList(),
                JogosDecisivos = itens
                    .Where(j => j.Decisivo)
                    .OrderBy(j => j.DataJogo)
                    .Take(6)
                    .ToList(),
                TimesEmAlta = MontarTimesEmAlta(itens)
            };

            return viewModel;
        }

        private async Task<List<int>> ObterRodadasAsync(int? campeonatoId)
        {
            var query = _context.Jogos
                .AsNoTracking()
                .Where(j => j.Ativo && j.Rodada.HasValue);

            if (campeonatoId.HasValue)
            {
                query = query.Where(j => j.CampeonatoId == campeonatoId.Value);
            }

            return await query
                .Select(j => j.Rodada!.Value)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
        }

        private async Task<List<Jogo>> ObterJogosFiltradosAsync(
            int? campeonatoId,
            int? rodada,
            DateTime? data,
            string periodo,
            DateTime hoje)
        {
            var baseQuery = _context.Jogos
                .AsNoTracking()
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j => j.Ativo);

            if (campeonatoId.HasValue)
            {
                baseQuery = baseQuery.Where(j => j.CampeonatoId == campeonatoId.Value);
            }

            if (data.HasValue)
            {
                return await baseQuery
                    .Where(j => j.DataJogo.Date == data.Value.Date)
                    .OrderBy(j => j.DataJogo)
                    .Take(24)
                    .ToListAsync();
            }

            if (rodada.HasValue)
            {
                return await baseQuery
                    .Where(j => j.Rodada == rodada.Value)
                    .OrderBy(j => j.DataJogo)
                    .Take(24)
                    .ToListAsync();
            }

            var somenteCampeonatoSelecionado = campeonatoId.HasValue &&
                !rodada.HasValue &&
                !data.HasValue &&
                string.IsNullOrWhiteSpace(periodo);

            if (periodo == PeriodoRodadaAtual || somenteCampeonatoSelecionado)
            {
                var rodadaAtual = await ObterRodadaMaisProximaAsync(baseQuery, hoje);

                if (rodadaAtual.HasValue)
                {
                    return await baseQuery
                        .Where(j => j.Rodada == rodadaAtual.Value)
                        .OrderBy(j => j.DataJogo)
                        .Take(24)
                        .ToListAsync();
                }
            }

            if (periodo == PeriodoHoje)
            {
                return await baseQuery
                    .Where(j => j.DataJogo.Date == hoje)
                    .OrderBy(j => j.DataJogo)
                    .Take(24)
                    .ToListAsync();
            }

            if (periodo == PeriodoProximos)
            {
                return await baseQuery
                    .Where(j => j.DataJogo.Date >= hoje && j.Status != "Finalizado")
                    .OrderBy(j => j.DataJogo)
                    .Take(24)
                    .ToListAsync();
            }

            if (periodo == PeriodoFinalizados)
            {
                return await baseQuery
                    .Where(j => j.Status == "Finalizado")
                    .OrderByDescending(j => j.DataJogo)
                    .Take(24)
                    .ToListAsync();
            }

            var jogosHoje = await baseQuery
                .Where(j => j.DataJogo.Date == hoje)
                .OrderBy(j => j.DataJogo)
                .Take(24)
                .ToListAsync();

            if (jogosHoje.Any())
            {
                return jogosHoje;
            }

            var proximos = await baseQuery
                .Where(j => j.DataJogo.Date >= hoje && j.Status != "Finalizado")
                .OrderBy(j => j.DataJogo)
                .Take(24)
                .ToListAsync();

            if (proximos.Any())
            {
                return proximos;
            }

            return await baseQuery
                .Where(j => j.Status == "Finalizado")
                .OrderByDescending(j => j.DataJogo)
                .Take(24)
                .ToListAsync();
        }

        private static async Task<int?> ObterRodadaMaisProximaAsync(
            IQueryable<Jogo> query,
            DateTime hoje)
        {
            var rodadas = await query
                .Where(j => j.Rodada.HasValue)
                .GroupBy(j => j.Rodada!.Value)
                .Select(g => new
                {
                    Rodada = g.Key,
                    DataReferencia = g.Min(j => j.DataJogo)
                })
                .ToListAsync();

            return rodadas
                .OrderBy(r => Math.Abs((r.DataReferencia.Date - hoje).TotalDays))
                .ThenBy(r => r.Rodada)
                .Select(r => (int?)r.Rodada)
                .FirstOrDefault();
        }

        private static RadarJogoItemViewModel MapearJogo(
            Jogo jogo,
            AnalisePartidaResultadoViewModel analise)
        {
            var previsao = analise.Previsao;
            var diferencaProbabilidades = new[]
            {
                Math.Abs(previsao.ProbabilidadeCasa - previsao.ProbabilidadeEmpate),
                Math.Abs(previsao.ProbabilidadeCasa - previsao.ProbabilidadeVisitante),
                Math.Abs(previsao.ProbabilidadeEmpate - previsao.ProbabilidadeVisitante)
            }.Min();
            var diferencaAproveitamento = Math.Abs(
                analise.Comparativo.Casa.Aproveitamento -
                analise.Comparativo.Visitante.Aproveitamento);
            var diferencaPontos = analise.ClassificacaoCasa != null && analise.ClassificacaoVisitante != null
                ? Math.Abs(analise.ClassificacaoCasa.Pontos - analise.ClassificacaoVisitante.Pontos)
                : (int?)null;
            var diferencaPosicao = analise.ClassificacaoCasa != null && analise.ClassificacaoVisitante != null
                ? Math.Abs(analise.ClassificacaoCasa.Posicao - analise.ClassificacaoVisitante.Posicao)
                : (int?)null;
            var equilibrio = CalcularIndicadorEquilibrio(
                diferencaProbabilidades,
                diferencaAproveitamento,
                diferencaPontos,
                diferencaPosicao);
            var favorito = Math.Max(previsao.ProbabilidadeCasa, previsao.ProbabilidadeVisitante) >= 45 &&
                Math.Abs(previsao.ProbabilidadeCasa - previsao.ProbabilidadeVisitante) >= 12;
            var mediaGolsProjetada = CalcularMediaGolsProjetada(analise);
            var indicadorGols = MontarIndicadorGols(mediaGolsProjetada, analise);
            var decisivo = EhDecisivo(jogo, analise, out var motivoDecisivo);
            var textoEquilibrio = equilibrio >= 72
                ? "Pouca diferença estatística entre os times"
                : previsao.Tendencia;

            return new RadarJogoItemViewModel
            {
                JogoId = jogo.Id,
                CampeonatoId = jogo.CampeonatoId,
                Campeonato = jogo.Campeonato?.Nome ?? "Campeonato",
                Rodada = jogo.Rodada,
                Fase = jogo.Fase,
                Grupo = jogo.Grupo,
                DataJogo = jogo.DataJogo,
                TimeCasa = jogo.TimeCasa?.Nome ?? "Mandante",
                TimeCasaId = jogo.TimeCasaId,
                TimeVisitante = jogo.TimeVisitante?.Nome ?? "Visitante",
                TimeVisitanteId = jogo.TimeVisitanteId,
                EscudoCasa = jogo.TimeCasa?.EscudoUrl,
                EscudoVisitante = jogo.TimeVisitante?.EscudoUrl,
                Status = jogo.Status,
                Placar = jogo.GolsCasa.HasValue && jogo.GolsVisitante.HasValue
                    ? $"{jogo.GolsCasa} x {jogo.GolsVisitante}"
                    : jogo.DataJogo.ToString("HH:mm"),
                ProbabilidadeCasa = previsao.ProbabilidadeCasa,
                ProbabilidadeEmpate = previsao.ProbabilidadeEmpate,
                ProbabilidadeVisitante = previsao.ProbabilidadeVisitante,
                Tendencia = previsao.Tendencia,
                IndicadorEquilibrio = equilibrio,
                IndicadorGols = indicadorGols,
                TextoResumo = equilibrio >= 72 ? textoEquilibrio : MontarResumo(previsao, indicadorGols),
                BadgePrincipal = equilibrio >= 72 ? "Jogo equilibrado" : previsao.Tendencia,
                Equilibrado = equilibrio >= 72,
                FavoritoClaro = favorito,
                TendenciaDeGols = mediaGolsProjetada >= 2.6 || mediaGolsProjetada <= 1.6,
                Decisivo = decisivo,
                MotivoDecisivo = motivoDecisivo,
                MediaGolsProjetada = mediaGolsProjetada,
                AproveitamentoCasa = analise.Comparativo.Casa.Aproveitamento,
                AproveitamentoVisitante = analise.Comparativo.Visitante.Aproveitamento,
                SaldoRecenteCasa = CalcularSaldoRecente(analise.UltimosJogosCasa),
                SaldoRecenteVisitante = CalcularSaldoRecente(analise.UltimosJogosVisitante),
                SequenciaSemPerderCasa = CalcularSequenciaSemPerder(analise.UltimosJogosCasa),
                SequenciaSemPerderVisitante = CalcularSequenciaSemPerder(analise.UltimosJogosVisitante),
                FormaCasa = analise.UltimosJogosCasa.Select(j => j.Resultado).ToList(),
                FormaVisitante = analise.UltimosJogosVisitante.Select(j => j.Resultado).ToList(),
                PosicaoCasa = analise.ClassificacaoCasa?.Posicao,
                PosicaoVisitante = analise.ClassificacaoVisitante?.Posicao,
                PontosCasa = analise.ClassificacaoCasa?.Pontos,
                PontosVisitante = analise.ClassificacaoVisitante?.Pontos
            };
        }

        private static int CalcularIndicadorEquilibrio(
            int diferencaProbabilidades,
            double diferencaAproveitamento,
            int? diferencaPontos,
            int? diferencaPosicao)
        {
            var score = 100 -
                diferencaProbabilidades -
                (int)Math.Round(diferencaAproveitamento * 0.35);

            if (diferencaPontos.HasValue)
            {
                score -= Math.Min(18, diferencaPontos.Value * 2);
            }

            if (diferencaPosicao.HasValue)
            {
                score -= Math.Min(15, diferencaPosicao.Value * 2);
            }

            return Math.Clamp(score, 0, 100);
        }

        private static double CalcularMediaGolsProjetada(AnalisePartidaResultadoViewModel analise)
        {
            var casa = analise.Comparativo.Casa;
            var visitante = analise.Comparativo.Visitante;
            var mediaCasa = casa.Jogos == 0
                ? 1.2
                : (casa.MediaGolsMarcados + casa.MediaGolsSofridos) / 2;
            var mediaVisitante = visitante.Jogos == 0
                ? 1.2
                : (visitante.MediaGolsMarcados + visitante.MediaGolsSofridos) / 2;

            return mediaCasa + mediaVisitante;
        }

        private static string MontarIndicadorGols(
            double mediaGolsProjetada,
            AnalisePartidaResultadoViewModel analise)
        {
            if (mediaGolsProjetada >= 3.1)
            {
                return "Tendência de jogo movimentado";
            }

            if (mediaGolsProjetada >= 2.6)
            {
                return "Times apresentam boa média ofensiva";
            }

            if (mediaGolsProjetada <= 1.6)
            {
                return "Jogo com tendência de poucos gols";
            }

            if (analise.Comparativo.Casa.MediaGolsSofridos >= 1.5 ||
                analise.Comparativo.Visitante.MediaGolsSofridos >= 1.5)
            {
                return "Defesas vêm sofrendo gols";
            }

            return "Tendência de gols moderada";
        }

        private static string MontarResumo(
            PrevisaoPartidaViewModel previsao,
            string indicadorGols)
        {
            return $"{previsao.Tendencia}. {indicadorGols}.";
        }

        private static bool EhDecisivo(
            Jogo jogo,
            AnalisePartidaResultadoViewModel analise,
            out string motivo)
        {
            motivo = string.Empty;
            var fase = jogo.Fase ?? string.Empty;

            if (fase.Contains("final", StringComparison.OrdinalIgnoreCase) ||
                fase.Contains("semi", StringComparison.OrdinalIgnoreCase) ||
                fase.Contains("quartas", StringComparison.OrdinalIgnoreCase) ||
                fase.Contains("mata", StringComparison.OrdinalIgnoreCase))
            {
                motivo = "Jogo de fase eliminatória";
                return true;
            }

            if (analise.ClassificacaoCasa == null || analise.ClassificacaoVisitante == null)
            {
                return false;
            }

            if (analise.ClassificacaoCasa.Posicao <= 4 && analise.ClassificacaoVisitante.Posicao <= 4)
            {
                motivo = "Confronto direto na parte de cima da tabela";
                return true;
            }

            if (Math.Abs(analise.ClassificacaoCasa.Pontos - analise.ClassificacaoVisitante.Pontos) <= 3 &&
                Math.Abs(analise.ClassificacaoCasa.Posicao - analise.ClassificacaoVisitante.Posicao) <= 4)
            {
                motivo = "Times próximos na classificação";
                return true;
            }

            if (!string.IsNullOrWhiteSpace(jogo.Grupo) &&
                Math.Abs(analise.ClassificacaoCasa.Posicao - analise.ClassificacaoVisitante.Posicao) <= 3)
            {
                motivo = "Duelo direto no mesmo grupo";
                return true;
            }

            return false;
        }

        private static List<RadarTimeMomentoViewModel> MontarTimesEmAlta(
            List<RadarJogoItemViewModel> jogos)
        {
            var candidatos = new List<RadarTimeMomentoViewModel>();

            foreach (var jogo in jogos)
            {
                candidatos.Add(new RadarTimeMomentoViewModel
                {
                    TimeId = jogo.TimeCasaId,
                    Nome = jogo.TimeCasa,
                    EscudoUrl = jogo.EscudoCasa,
                    AproveitamentoRecente = jogo.AproveitamentoCasa,
                    SaldoRecente = jogo.SaldoRecenteCasa,
                    SequenciaSemPerder = jogo.SequenciaSemPerderCasa,
                    ProximoAdversario = jogo.TimeVisitante,
                    JogoId = jogo.JogoId,
                    Forma = jogo.FormaCasa
                });

                candidatos.Add(new RadarTimeMomentoViewModel
                {
                    TimeId = jogo.TimeVisitanteId,
                    Nome = jogo.TimeVisitante,
                    EscudoUrl = jogo.EscudoVisitante,
                    AproveitamentoRecente = jogo.AproveitamentoVisitante,
                    SaldoRecente = jogo.SaldoRecenteVisitante,
                    SequenciaSemPerder = jogo.SequenciaSemPerderVisitante,
                    ProximoAdversario = jogo.TimeCasa,
                    JogoId = jogo.JogoId,
                    Forma = jogo.FormaVisitante
                });
            }

            return candidatos
                .GroupBy(t => t.Nome)
                .Select(g => g.OrderByDescending(t => t.AproveitamentoRecente).First())
                .Where(t => t.AproveitamentoRecente >= 50)
                .OrderByDescending(t => t.AproveitamentoRecente)
                .Take(6)
                .ToList();
        }

        private static int CalcularSequenciaSemPerder(List<UltimoJogoTimeViewModel> jogos)
        {
            var sequencia = 0;

            foreach (var jogo in jogos)
            {
                if (jogo.Resultado == "D")
                {
                    break;
                }

                sequencia++;
            }

            return sequencia;
        }

        private static int CalcularSaldoRecente(List<UltimoJogoTimeViewModel> jogos)
        {
            var saldo = 0;

            foreach (var jogo in jogos)
            {
                var partes = jogo.Placar.Split(" x ");

                if (partes.Length == 2 &&
                    int.TryParse(partes[0], out var golsCasa) &&
                    int.TryParse(partes[1], out var golsVisitante))
                {
                    saldo += jogo.Mandante
                        ? golsCasa - golsVisitante
                        : golsVisitante - golsCasa;
                }
            }

            return saldo;
        }

        private static string MontarTituloContexto(
            List<Campeonato> campeonatos,
            int? campeonatoId,
            int? rodada,
            DateTime? data,
            string periodo,
            List<RadarJogoItemViewModel> jogos)
        {
            if (data.HasValue)
            {
                return $"Radar de {data.Value:dd/MM/yyyy}";
            }

            if (campeonatoId.HasValue && rodada.HasValue)
            {
                var campeonato = campeonatos.FirstOrDefault(c => c.Id == campeonatoId.Value);
                return $"{campeonato?.Nome ?? "Campeonato"} - {rodada}ª rodada";
            }

            if (campeonatoId.HasValue)
            {
                var campeonato = campeonatos.FirstOrDefault(c => c.Id == campeonatoId.Value);
                var rodadaRadar = jogos.Select(j => j.Rodada).FirstOrDefault(r => r.HasValue);
                return rodadaRadar.HasValue
                    ? $"{campeonato?.Nome ?? "Campeonato"} - {rodadaRadar}ª rodada"
                    : campeonato?.Nome ?? "Radar da Rodada";
            }

            return periodo switch
            {
                PeriodoHoje => "Jogos de hoje",
                PeriodoProximos => "Próximos jogos",
                PeriodoFinalizados => "Finalizados recentes",
                PeriodoRodadaAtual => "Rodada atual",
                _ => "Radar da Rodada"
            };
        }

        private static string ResolverPeriodo(
            string periodo,
            bool somenteCampeonatoSelecionado,
            DateTime? data,
            int? rodada,
            int? campeonatoId,
            List<RadarJogoItemViewModel> jogos)
        {
            if (data.HasValue)
            {
                return "Data selecionada";
            }

            if (rodada.HasValue)
            {
                return "Rodada selecionada";
            }

            if (campeonatoId.HasValue && somenteCampeonatoSelecionado)
            {
                return "Rodada mais próxima";
            }

            if (!string.IsNullOrWhiteSpace(periodo))
            {
                return periodo switch
                {
                    PeriodoHoje => "Hoje",
                    PeriodoProximos => "Próximos jogos",
                    PeriodoFinalizados => "Finalizados recentes",
                    PeriodoRodadaAtual => "Rodada atual",
                    _ => "Período selecionado"
                };
            }

            return jogos.Any(j => j.DataJogo.Date == DateTime.Today)
                ? "Hoje"
                : "Próximos jogos ou finalizados recentes";
        }

        private static string NormalizarPeriodo(string? periodo)
        {
            return periodo?.ToLowerInvariant() switch
            {
                PeriodoHoje => PeriodoHoje,
                PeriodoProximos => PeriodoProximos,
                PeriodoFinalizados => PeriodoFinalizados,
                PeriodoRodadaAtual => PeriodoRodadaAtual,
                _ => string.Empty
            };
        }

        private static bool EhFinalizado(string status)
        {
            return string.Equals(status, "Finalizado", StringComparison.OrdinalIgnoreCase);
        }
    }
}
