using FutPlay.Data;
using FutPlay.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FutPlay.Services
{
    public class ImportacaoResultadosService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;
        private readonly AppTimeService _appTimeService;
        private readonly ILogger<ImportacaoResultadosService> _logger;

        public ImportacaoResultadosService(
            FootballApiService footballApiService,
            AppDbContext context,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService,
            AppTimeService appTimeService,
            ILogger<ImportacaoResultadosService> logger)
        {
            _footballApiService = footballApiService;
            _context = context;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
            _appTimeService = appTimeService;
            _logger = logger;
        }

        public async Task<ImportacaoResultadosResultado> ImportarResultadosAsync(int campeonatoId)
        {
            _logger.LogInformation("Iniciando importacao de resultados. CampeonatoId: {CampeonatoId}", campeonatoId);

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                return ImportacaoResultadosResultado.Falha("Campeonato não encontrado.");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                return ImportacaoResultadosResultado.Falha("Este campeonato não possui ID da API-Football.");
            }

            try
            {
                using var resultado = await _footballApiService.BuscarJogosAsync(
                    campeonato.ApiLeagueId.Value,
                    campeonato.Ano);

                var jogosCampeonato = await _context.Jogos
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j => j.CampeonatoId == campeonato.Id)
                    .ToListAsync();

                var jogosPorApiFixture = jogosCampeonato
                    .Where(j => j.ApiFixtureId.HasValue)
                    .GroupBy(j => j.ApiFixtureId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var jogosSemApiPorTimes = jogosCampeonato
                    .Where(j => !j.ApiFixtureId.HasValue && j.TimeCasa != null && j.TimeVisitante != null)
                    .GroupBy(j => CriarChaveTimes(j.TimeCasa!.Nome, j.TimeVisitante!.Nome))
                    .ToDictionary(g => g.Key, g => g.First());

                int jogosAtualizados = 0;
                int jogosVinculados = 0;

                if (resultado.RootElement.TryGetProperty("response", out var response))
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var fixture = item.GetProperty("fixture");
                        var teams = item.GetProperty("teams");
                        var goals = item.GetProperty("goals");

                        int apiFixtureId = fixture.GetProperty("id").GetInt32();

                        var teamHome = teams.GetProperty("home");
                        var teamAway = teams.GetProperty("away");

                        string nomeTimeCasa = teamHome.GetProperty("name").GetString() ?? string.Empty;
                        string nomeTimeVisitante = teamAway.GetProperty("name").GetString() ?? string.Empty;

                        string? chaveTimesEncontrada = null;

                        var jogo = jogosPorApiFixture.TryGetValue(apiFixtureId, out var jogoPorFixture)
                            ? jogoPorFixture
                            : null;

                        if (jogo == null)
                        {
                            var chaveTimes = CriarChaveTimes(nomeTimeCasa, nomeTimeVisitante);
                            if (jogosSemApiPorTimes.TryGetValue(chaveTimes, out jogo))
                            {
                                chaveTimesEncontrada = chaveTimes;
                            }
                        }

                        if (jogo == null)
                        {
                            continue;
                        }

                        bool mudou = false;

                        if (jogo.ApiFixtureId != apiFixtureId)
                        {
                            jogo.ApiFixtureId = apiFixtureId;
                            jogosPorApiFixture[apiFixtureId] = jogo;

                            if (chaveTimesEncontrada != null)
                            {
                                jogosSemApiPorTimes.Remove(chaveTimesEncontrada);
                            }

                            jogosVinculados++;
                            mudou = true;
                        }

                        DateTime dataJogo = _appTimeService.ConverterUtcParaHorarioAplicacao(
                            fixture.GetProperty("date").GetDateTime());

                        if (jogo.DataJogo != dataJogo)
                        {
                            jogo.DataJogo = dataJogo;
                            mudou = true;
                        }

                        string statusApi = fixture
                            .GetProperty("status")
                            .GetProperty("short")
                            .GetString() ?? string.Empty;

                        string status = string.Equals(statusApi, "FT", StringComparison.OrdinalIgnoreCase)
                            ? "Finalizado"
                            : FootballApiStatusMapper.ConverterStatusJogo(statusApi);

                        if (!string.Equals(jogo.Status, status, StringComparison.OrdinalIgnoreCase))
                        {
                            jogo.Status = status;
                            mudou = true;
                        }

                        var golsCasa = ObterInteiroNullable(goals, "home");
                        var golsVisitante = ObterInteiroNullable(goals, "away");

                        if (golsCasa.HasValue && jogo.GolsCasa != golsCasa.Value)
                        {
                            jogo.GolsCasa = golsCasa.Value;
                            mudou = true;
                        }

                        if (golsVisitante.HasValue && jogo.GolsVisitante != golsVisitante.Value)
                        {
                            jogo.GolsVisitante = golsVisitante.Value;
                            mudou = true;
                        }

                        if (mudou)
                        {
                            jogosAtualizados++;
                        }
                    }

                    if (jogosAtualizados > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
                await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);

                _logger.LogInformation(
                    "Importacao de resultados concluida. CampeonatoId: {CampeonatoId}. JogosAtualizados: {JogosAtualizados}",
                    campeonato.Id,
                    jogosAtualizados);

                return ImportacaoResultadosResultado.Ok(jogosAtualizados, jogosVinculados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar resultados. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return ImportacaoResultadosResultado.Falha($"Erro ao importar resultados: {ex.Message}");
            }
        }

        private static int? ObterInteiroNullable(JsonElement elemento, string propriedade)
        {
            if (elemento.TryGetProperty(propriedade, out var valor) &&
                valor.ValueKind != JsonValueKind.Null)
            {
                return valor.GetInt32();
            }

            return null;
        }

        private static string CriarChaveTimes(string timeCasa, string timeVisitante)
        {
            return $"{NormalizarNome(timeCasa)}|{NormalizarNome(timeVisitante)}";
        }

        private static string NormalizarNome(string nome)
        {
            var normalizado = nome.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalizado.Length);

            foreach (var caractere in normalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(caractere);

                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToUpperInvariant(caractere));
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }
    }

    public class ImportacaoResultadosResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public int JogosAtualizados { get; set; }

        public int JogosVinculados { get; set; }

        public static ImportacaoResultadosResultado Ok(int jogosAtualizados, int jogosVinculados)
        {
            return new ImportacaoResultadosResultado
            {
                Sucesso = true,
                JogosAtualizados = jogosAtualizados,
                JogosVinculados = jogosVinculados,
                Mensagem = $"Importação de resultados concluída. Jogos atualizados: {jogosAtualizados}. Vínculos por times: {jogosVinculados}. Classificação e palpites recalculados."
            };
        }

        public static ImportacaoResultadosResultado Falha(string mensagem)
        {
            return new ImportacaoResultadosResultado
            {
                Sucesso = false,
                Mensagem = mensagem
            };
        }
    }
}
