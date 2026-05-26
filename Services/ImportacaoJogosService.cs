using FutPlay.Data;
using FutPlay.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FutPlay.Services
{
    public class ImportacaoJogosService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;
        private readonly AppTimeService _appTimeService;
        private readonly ILogger<ImportacaoJogosService> _logger;

        public ImportacaoJogosService(
            FootballApiService footballApiService,
            AppDbContext context,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService,
            AppTimeService appTimeService,
            ILogger<ImportacaoJogosService> logger)
        {
            _footballApiService = footballApiService;
            _context = context;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
            _appTimeService = appTimeService;
            _logger = logger;
        }

        public async Task<ImportacaoJogosResultado> ImportarJogosAsync(int campeonatoId)
        {
            _logger.LogInformation("Iniciando importacao de jogos. CampeonatoId: {CampeonatoId}", campeonatoId);

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                _logger.LogWarning("Campeonato nao encontrado para importacao de jogos. CampeonatoId: {CampeonatoId}", campeonatoId);
                return ImportacaoJogosResultado.Falha("Campeonato não encontrado.");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                _logger.LogWarning("Campeonato sem ApiLeagueId para importacao de jogos. CampeonatoId: {CampeonatoId}", campeonatoId);
                return ImportacaoJogosResultado.Falha("Este campeonato não possui ID da API.");
            }

            try
            {
                using var resultado = await _footballApiService.BuscarJogosAsync(
                    campeonato.ApiLeagueId.Value,
                    campeonato.Ano
                );

                int jogosImportados = 0;
                int timesImportados = 0;
                bool resultadosImportados = false;

                if (resultado.RootElement.TryGetProperty("response", out var response))
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var fixture = item.GetProperty("fixture");
                        var teams = item.GetProperty("teams");
                        var goals = item.GetProperty("goals");
                        var league = item.GetProperty("league");

                        int apiFixtureId = fixture.GetProperty("id").GetInt32();

                        bool jogoJaExiste = await _context.Jogos
                            .AnyAsync(j => j.ApiFixtureId == apiFixtureId);

                        if (jogoJaExiste)
                        {
                            continue;
                        }

                        var teamHome = teams.GetProperty("home");
                        var teamAway = teams.GetProperty("away");

                        int apiTimeCasaId = teamHome.GetProperty("id").GetInt32();
                        int apiTimeVisitanteId = teamAway.GetProperty("id").GetInt32();

                        string nomeTimeCasa = teamHome.GetProperty("name").GetString() ?? "";
                        string nomeTimeVisitante = teamAway.GetProperty("name").GetString() ?? "";

                        string? logoCasa = teamHome.TryGetProperty("logo", out var logoCasaElement)
                            ? logoCasaElement.GetString()
                            : null;

                        string? logoVisitante = teamAway.TryGetProperty("logo", out var logoVisitanteElement)
                            ? logoVisitanteElement.GetString()
                            : null;

                        var timeCasa = await ObterOuCriarTimeApi(
                            apiTimeCasaId,
                            nomeTimeCasa,
                            logoCasa
                        );

                        if (timeCasa.Criado)
                        {
                            timesImportados++;
                        }

                        var timeVisitante = await ObterOuCriarTimeApi(
                            apiTimeVisitanteId,
                            nomeTimeVisitante,
                            logoVisitante
                        );

                        if (timeVisitante.Criado)
                        {
                            timesImportados++;
                        }

                        DateTime dataJogo = _appTimeService.ConverterUtcParaHorarioAplicacao(
                            fixture.GetProperty("date").GetDateTime());

                        string statusApi = fixture
                            .GetProperty("status")
                            .GetProperty("short")
                            .GetString() ?? "";

                        string status = FootballApiStatusMapper.ConverterStatusJogo(statusApi);

                        int? golsCasa = null;
                        int? golsVisitante = null;

                        if (goals.TryGetProperty("home", out var golsCasaElement) &&
                            golsCasaElement.ValueKind != JsonValueKind.Null)
                        {
                            golsCasa = golsCasaElement.GetInt32();
                        }

                        if (goals.TryGetProperty("away", out var golsVisitanteElement) &&
                            golsVisitanteElement.ValueKind != JsonValueKind.Null)
                        {
                            golsVisitante = golsVisitanteElement.GetInt32();
                        }

                        string? rodada = league.TryGetProperty("round", out var roundElement)
                            ? roundElement.GetString()
                            : null;

                        var jogo = new Jogo
                        {
                            CampeonatoId = campeonato.Id,
                            TimeCasaId = timeCasa.Time.Id,
                            TimeVisitanteId = timeVisitante.Time.Id,
                            DataJogo = dataJogo,
                            Fase = rodada,
                            Grupo = ExtrairGrupo(rodada),
                            Rodada = ExtrairNumeroRodada(rodada),
                            GolsCasa = golsCasa,
                            GolsVisitante = golsVisitante,
                            Status = status,
                            Ativo = true,
                            ApiFixtureId = apiFixtureId
                        };

                        _context.Jogos.Add(jogo);
                        jogosImportados++;

                        if (JogoAfetaClassificacao(jogo))
                        {
                            resultadosImportados = true;
                        }
                    }

                    await _context.SaveChangesAsync();

                    if (resultadosImportados)
                    {
                        await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
                        await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);
                    }
                }

                _logger.LogInformation("Importacao de jogos concluida com sucesso. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return ImportacaoJogosResultado.Ok(jogosImportados, timesImportados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar jogos. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return ImportacaoJogosResultado.Falha($"Erro ao importar jogos: {ex.Message}");
            }
        }

        private async Task<(Time Time, bool Criado)> ObterOuCriarTimeApi(
            int apiTeamId,
            string nome,
            string? logoUrl)
        {
            var time = await _context.Times
                .FirstOrDefaultAsync(t => t.ApiTeamId == apiTeamId);

            if (time != null)
            {
                return (time, false);
            }

            time = new Time
            {
                Nome = nome,
                Sigla = GerarSigla(nome),
                Pais = null,
                Tipo = "Clube",
                EscudoUrl = logoUrl,
                Ativo = true,
                ApiTeamId = apiTeamId
            };

            _context.Times.Add(time);

            await _context.SaveChangesAsync();

            return (time, true);
        }

        private string GerarSigla(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "";
            }

            var partes = nome
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(3)
                .ToList();

            if (partes.Count == 1)
            {
                return partes[0].Length >= 3
                    ? partes[0].Substring(0, 3).ToUpper()
                    : partes[0].ToUpper();
            }

            return string.Join("", partes.Select(p => p[0])).ToUpper();
        }

        private static bool JogoAfetaClassificacao(Jogo jogo)
        {
            return jogo.Ativo &&
                   string.Equals(jogo.Status, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                   jogo.GolsCasa.HasValue &&
                   jogo.GolsVisitante.HasValue;
        }

        private static int? ExtrairNumeroRodada(string? rodada)
        {
            if (string.IsNullOrWhiteSpace(rodada))
            {
                return null;
            }

            var match = Regex.Match(rodada, @"(?<!\d)(\d{1,3})(?!\d)");

            return match.Success && int.TryParse(match.Groups[1].Value, out var numero)
                ? numero
                : null;
        }

        private static string? ExtrairGrupo(string? rodada)
        {
            if (string.IsNullOrWhiteSpace(rodada))
            {
                return null;
            }

            var match = Regex.Match(rodada, @"(?:Group|Grupo)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value.ToUpperInvariant()
                : null;
        }
    }

    public class ImportacaoJogosResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public int JogosImportados { get; set; }

        public int TimesImportados { get; set; }

        public static ImportacaoJogosResultado Ok(int jogosImportados, int timesImportados)
        {
            return new ImportacaoJogosResultado
            {
                Sucesso = true,
                JogosImportados = jogosImportados,
                TimesImportados = timesImportados,
                Mensagem = $"Importação concluída. Jogos importados: {jogosImportados}. Times importados: {timesImportados}."
            };
        }

        public static ImportacaoJogosResultado Falha(string mensagem)
        {
            return new ImportacaoJogosResultado
            {
                Sucesso = false,
                Mensagem = mensagem
            };
        }
    }
}
