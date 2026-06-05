using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Models.Api;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FutPlay.Services
{
    public class FootballDataOrgService
    {
        private const string Fonte = "football-data.org";

        private readonly HttpClient _httpClient;
        private readonly FootballDataOrgOptions _options;
        private readonly AppDbContext _context;
        private readonly AppTimeService _appTimeService;
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;
        private readonly ApiSyncLogService _apiSyncLogService;
        private readonly ILogger<FootballDataOrgService> _logger;

        public FootballDataOrgService(
            HttpClient httpClient,
            IOptions<FootballDataOrgOptions> options,
            AppDbContext context,
            AppTimeService appTimeService,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService,
            ApiSyncLogService apiSyncLogService,
            ILogger<FootballDataOrgService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _context = context;
            _appTimeService = appTimeService;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
            _apiSyncLogService = apiSyncLogService;
            _logger = logger;

            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? "https://api.football-data.org/v4"
                : _options.BaseUrl.Trim();

            _httpClient.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/");

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Auth-Token", _options.ApiKey);
            }
        }

        public async Task<List<FootballDataOrgCompeticaoViewModel>> ListarCompeticoesAsync()
        {
            using var consulta = await ConsultarApiAsync("competitions/", "competições");
            var competicoes = new List<FootballDataOrgCompeticaoViewModel>();

            if (!consulta.Documento.RootElement.TryGetProperty("competitions", out var lista))
            {
                return competicoes;
            }

            foreach (var item in lista.EnumerateArray())
            {
                competicoes.Add(ExtrairCompeticao(item));
            }

            return competicoes
                .OrderBy(c => c.Pais)
                .ThenBy(c => c.Nome)
                .ToList();
        }

        public async Task<FootballDataOrgCompetitionValidationResult> ValidarCompeticaoCampeonatoAsync(int campeonatoId)
        {
            var campeonato = await _context.Campeonatos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                return FootballDataOrgCompetitionValidationResult.Falha(
                    "Campeonato local não encontrado.",
                    endpoint: null,
                    statusCode: null);
            }

            var idOuCode = ObterIdentificadorCompeticao(campeonato);

            if (string.IsNullOrWhiteSpace(idOuCode))
            {
                return FootballDataOrgCompetitionValidationResult.Falha(
                    "Campeonato não vinculado ao football-data.org.",
                    endpoint: null,
                    statusCode: null);
            }

            return await ValidarCompeticaoAsync(idOuCode);
        }

        public async Task<FootballDataOrgCompetitionValidationResult> ValidarCompeticaoAsync(string idOuCode)
        {
            var identificador = NormalizarIdentificadorCompeticao(idOuCode);

            if (string.IsNullOrWhiteSpace(identificador))
            {
                return FootballDataOrgCompetitionValidationResult.Falha(
                    "Informe o Competition ID ou code do football-data.org.",
                    endpoint: null,
                    statusCode: null);
            }

            var endpointRelativo = $"competitions/{Uri.EscapeDataString(identificador)}";
            var endpoint = CriarEndpointResumo(endpointRelativo);

            try
            {
                using var consulta = await ConsultarApiAsync(endpointRelativo, $"competição {identificador}");
                var competicao = ExtrairCompeticao(consulta.Documento.RootElement);
                var resultado = FootballDataOrgCompetitionValidationResult.Ok(
                    endpoint,
                    (int)consulta.StatusCode,
                    competicao);

                resultado.TemporadasDisponiveis = ExtrairTemporadasDisponiveis(consulta.Documento.RootElement);

                return resultado;
            }
            catch (FootballDataOrgHttpException ex)
            {
                return FootballDataOrgCompetitionValidationResult.Falha(
                    ex.UserMessage,
                    ex.Endpoint,
                    (int)ex.StatusCode);
            }
        }

        public async Task<List<FootballDataOrgMatchData>> BuscarJogosCompeticaoAsync(
            int competitionId,
            int temporada)
        {
            var resposta = await BuscarJogosCompeticaoComDetalhesAsync(
                competitionId.ToString(CultureInfo.InvariantCulture),
                temporada);

            return resposta.Jogos;
        }

        public async Task<List<FootballDataOrgMatchData>> BuscarJogosCompeticaoAsync(
            string competitionIdentifier,
            int temporada)
        {
            var resposta = await BuscarJogosCompeticaoComDetalhesAsync(competitionIdentifier, temporada);

            return resposta.Jogos;
        }

        public Task<FootballDataOrgSyncResultado> AtualizarResultadosAsync(
            int campeonatoId,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            return AtualizarResultadosAsync(
                campeonatoId,
                footballDataCompetitionId: null,
                competitionCode: null,
                temporada: null,
                usuarioId,
                usuarioEmail);
        }

        public async Task<FootballDataOrgSyncResultado> AtualizarResultadosAsync(
            int campeonatoId,
            int? footballDataCompetitionId,
            string? competitionCode,
            int? temporada,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            var inicio = DateTime.UtcNow;
            var resultado = FootballDataOrgSyncResultado.Iniciar(campeonatoId);

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                return FootballDataOrgSyncResultado.Falha("Campeonato não encontrado.", campeonatoId);
            }

            resultado.CampeonatoNome = $"{campeonato.Nome} {campeonato.Ano}".Trim();

            AplicarConfiguracaoFootballData(campeonato, footballDataCompetitionId, competitionCode, temporada);

            var idOuCode = ObterIdentificadorCompeticao(campeonato);

            if (string.IsNullOrWhiteSpace(idOuCode))
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Campeonato não vinculado ao football-data.org.";
                resultado.Erros.Add(resultado.Mensagem);

                await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail);
                return resultado;
            }

            resultado.FootballDataCompetitionId = campeonato.FootballDataCompetitionId;
            resultado.FootballDataCompetitionCode = campeonato.FootballDataCompetitionCode;

            try
            {
                var validacao = await ValidarCompeticaoAsync(idOuCode);
                resultado.EndpointValidacao = validacao.Endpoint;
                resultado.StatusHttpValidacao = validacao.StatusHttp;
                resultado.CompeticaoFootballDataNome = validacao.Nome;
                resultado.CompeticaoFootballDataCode = validacao.Codigo;
                resultado.CompeticaoFootballDataId = validacao.Id;

                if (!validacao.Sucesso)
                {
                    resultado.Sucesso = false;
                    resultado.Erros.Add(validacao.Mensagem);
                    resultado.Mensagem = MontarMensagemSincronizacao(resultado);

                    await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail);
                    return resultado;
                }

                AtualizarVinculoComCompeticaoValidada(campeonato, validacao);

                var temporadaSincronizacao = campeonato.FootballDataSeason
                    ?? validacao.TemporadaAtual
                    ?? campeonato.Ano;

                if (!campeonato.FootballDataSeason.HasValue)
                {
                    campeonato.FootballDataSeason = temporadaSincronizacao;
                }

                resultado.FootballDataCompetitionId = campeonato.FootballDataCompetitionId;
                resultado.FootballDataCompetitionCode = campeonato.FootballDataCompetitionCode;
                resultado.Temporada = temporadaSincronizacao;

                if (temporadaSincronizacao is < 1900 or > 2100)
                {
                    resultado.Sucesso = false;
                    resultado.Erros.Add("FootballDataSeason inválida para consulta no football-data.org.");
                    resultado.Mensagem = MontarMensagemSincronizacao(resultado);

                    await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail);
                    return resultado;
                }

                if (validacao.TemporadasDisponiveis.Any() &&
                    !validacao.TemporadasDisponiveis.Contains(temporadaSincronizacao))
                {
                    resultado.Avisos.Add(
                        $"FootballDataSeason {temporadaSincronizacao} não aparece nas temporadas retornadas pela competição validada.");
                }

                if (_context.ChangeTracker.HasChanges())
                {
                    await _context.SaveChangesAsync();
                }

                var jogosApi = await BuscarJogosCompeticaoComDetalhesAsync(idOuCode, temporadaSincronizacao);
                resultado.EndpointJogos = jogosApi.Endpoint;
                resultado.StatusHttpJogos = jogosApi.StatusHttp;
                resultado.JogosRetornadosApi = jogosApi.Jogos.Count;
                resultado.TotalProcessados = jogosApi.Jogos.Count;

                if (!jogosApi.Jogos.Any())
                {
                    resultado.Sucesso = false;
                    resultado.Erros.Add("Nenhum jogo retornado para essa competição/temporada/filtros.");
                    resultado.Mensagem = MontarMensagemSincronizacao(resultado);

                    await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail);
                    return resultado;
                }

                var jogosLocais = await _context.Jogos
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j => j.CampeonatoId == campeonato.Id && j.Ativo)
                    .ToListAsync();

                resultado.JogosLocaisBanco = jogosLocais.Count;
                resultado.JogosLocaisComFootballDataMatchId = jogosLocais.Count(j => j.FootballDataMatchId.HasValue);

                var jogosPorFootballDataMatchId = CriarMapaJogosPorFootballDataMatchId(jogosLocais, resultado);

                foreach (var jogoApi in jogosApi.Jogos)
                {
                    try
                    {
                        ProcessarJogoApi(
                            jogosLocais,
                            jogosPorFootballDataMatchId,
                            jogoApi,
                            resultado);
                    }
                    catch (Exception ex)
                    {
                        resultado.Erros.Add($"Match {jogoApi.MatchId}: {ex.Message}");
                        _logger.LogError(
                            ex,
                            "Erro ao atualizar resultado via football-data.org. CampeonatoId: {CampeonatoId}. FootballDataMatchId: {FootballDataMatchId}",
                            campeonato.Id,
                            jogoApi.MatchId);
                    }
                }

                await _context.SaveChangesAsync();

                if (resultado.JogosAtualizados > 0)
                {
                    await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
                    await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);
                    resultado.ClassificacaoRecalculada = true;
                    resultado.RankingRecalculado = true;
                }

                resultado.Sucesso = !resultado.Erros.Any();
                resultado.Mensagem = MontarMensagemSincronizacao(resultado);

                await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail);

                return resultado;
            }
            catch (FootballDataOrgHttpException ex)
            {
                resultado.Sucesso = false;
                resultado.EndpointJogos = ex.Endpoint;
                resultado.StatusHttpJogos = (int)ex.StatusCode;
                resultado.Erros.Add(ex.UserMessage);
                resultado.Mensagem = MontarMensagemSincronizacao(resultado);

                await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail, ex.ToString());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao sincronizar resultados via football-data.org. CampeonatoId: {CampeonatoId}. FootballDataCompetitionId: {FootballDataCompetitionId}. Code: {Code}. Temporada: {Temporada}",
                    campeonato.Id,
                    campeonato.FootballDataCompetitionId,
                    campeonato.FootballDataCompetitionCode,
                    resultado.Temporada);

                resultado.Sucesso = false;
                resultado.Mensagem = $"Erro ao sincronizar via {Fonte}: {ex.Message}";
                resultado.Erros.Add(ex.Message);

                await RegistrarLogAsync(campeonato, inicio, resultado, usuarioId, usuarioEmail, ex.ToString());

                return resultado;
            }
        }

        private async Task<FootballDataOrgMatchesResponse> BuscarJogosCompeticaoComDetalhesAsync(
            string competitionIdentifier,
            int temporada)
        {
            var identificador = NormalizarIdentificadorCompeticao(competitionIdentifier);

            if (string.IsNullOrWhiteSpace(identificador))
            {
                throw new InvalidOperationException("Informe o Competition ID ou code do football-data.org.");
            }

            var endpointRelativo = $"competitions/{Uri.EscapeDataString(identificador)}/matches?season={temporada}";

            using var consulta = await ConsultarApiAsync(
                endpointRelativo,
                $"jogos {identificador}/{temporada}");

            var jogos = new List<FootballDataOrgMatchData>();

            if (!consulta.Documento.RootElement.TryGetProperty("matches", out var matches))
            {
                return new FootballDataOrgMatchesResponse
                {
                    Jogos = jogos,
                    Endpoint = consulta.Endpoint,
                    StatusHttp = (int)consulta.StatusCode
                };
            }

            foreach (var item in matches.EnumerateArray())
            {
                jogos.Add(ExtrairJogo(item));
            }

            return new FootballDataOrgMatchesResponse
            {
                Jogos = jogos,
                Endpoint = consulta.Endpoint,
                StatusHttp = (int)consulta.StatusCode
            };
        }

        private void ProcessarJogoApi(
            List<Jogo> jogosLocais,
            Dictionary<int, Jogo> jogosPorFootballDataMatchId,
            FootballDataOrgMatchData jogoApi,
            FootballDataOrgSyncResultado resultado)
        {
            if (jogoApi.MatchId <= 0)
            {
                resultado.AdicionarIgnorado("API retornou jogo sem id");
                return;
            }

            var vinculadoNestaExecucao = false;

            if (!jogosPorFootballDataMatchId.TryGetValue(jogoApi.MatchId, out var jogoLocal))
            {
                jogoLocal = LocalizarJogoParaVinculo(jogosLocais, jogoApi, out var motivoIgnorado);

                if (jogoLocal == null)
                {
                    resultado.AdicionarIgnorado(motivoIgnorado);
                    return;
                }

                jogoLocal.FootballDataMatchId = jogoApi.MatchId;
                jogosPorFootballDataMatchId[jogoApi.MatchId] = jogoLocal;
                resultado.JogosVinculadosFootballData++;
                vinculadoNestaExecucao = true;
            }

            resultado.JogosEncontradosBanco++;

            if (!AplicarDadosJogo(jogoLocal, jogoApi))
            {
                if (!vinculadoNestaExecucao)
                {
                    resultado.AdicionarIgnorado("Jogo encontrado sem alterações");
                }

                return;
            }

            resultado.JogosAtualizados++;

            if (jogoApi.EstaFinalizado)
            {
                resultado.JogosFinalizados++;
            }
        }

        private static Dictionary<int, Jogo> CriarMapaJogosPorFootballDataMatchId(
            List<Jogo> jogosLocais,
            FootballDataOrgSyncResultado resultado)
        {
            var grupos = jogosLocais
                .Where(j => j.FootballDataMatchId.HasValue)
                .GroupBy(j => j.FootballDataMatchId!.Value)
                .ToList();

            foreach (var grupo in grupos.Where(g => g.Count() > 1))
            {
                resultado.Erros.Add(
                    $"FootballDataMatchId duplicado no banco: {grupo.Key} ({grupo.Count()} jogos locais).");
            }

            return grupos.ToDictionary(g => g.Key, g => g.First());
        }

        private async Task<FootballDataOrgApiResponse> ConsultarApiAsync(string url, string recurso)
        {
            var endpoint = CriarEndpointResumo(url);

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new FootballDataOrgHttpException(
                    HttpStatusCode.Unauthorized,
                    endpoint,
                    "Chave football-data.org inválida ou não configurada.");
            }

            using var response = await _httpClient.GetAsync(url);
            var statusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                throw new FootballDataOrgHttpException(
                    statusCode,
                    endpoint,
                    MapearMensagemStatusHttp(statusCode));
            }

            var json = await response.Content.ReadAsStringAsync();

            return new FootballDataOrgApiResponse(
                JsonDocument.Parse(json),
                statusCode,
                endpoint);
        }

        private FootballDataOrgMatchData ExtrairJogo(JsonElement item)
        {
            var homeTeam = item.GetProperty("homeTeam");
            var awayTeam = item.GetProperty("awayTeam");
            var score = item.GetProperty("score");
            var fullTime = score.GetProperty("fullTime");
            var utcDate = DateTime.Parse(
                ObterString(item, "utcDate") ?? string.Empty,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var statusApi = ObterString(item, "status") ?? string.Empty;

            return new FootballDataOrgMatchData
            {
                MatchId = ObterInt(item, "id") ?? 0,
                DataJogo = _appTimeService.ConverterUtcParaHorarioAplicacao(utcDate),
                StatusApi = statusApi,
                Status = ConverterStatus(statusApi),
                Matchday = ObterInt(item, "matchday"),
                Stage = ObterString(item, "stage"),
                Grupo = ObterString(item, "group"),
                TimeCasa = ExtrairTime(homeTeam),
                TimeVisitante = ExtrairTime(awayTeam),
                GolsCasa = ObterInt(fullTime, "home"),
                GolsVisitante = ObterInt(fullTime, "away")
            };
        }

        private static FootballDataOrgCompeticaoViewModel ExtrairCompeticao(JsonElement item)
        {
            var currentSeason = item.TryGetProperty("currentSeason", out var season) &&
                season.ValueKind == JsonValueKind.Object
                    ? season
                    : default;

            var startDate = currentSeason.ValueKind == JsonValueKind.Object
                ? ObterData(currentSeason, "startDate")
                : null;
            var endDate = currentSeason.ValueKind == JsonValueKind.Object
                ? ObterData(currentSeason, "endDate")
                : null;

            return new FootballDataOrgCompeticaoViewModel
            {
                Id = ObterInt(item, "id") ?? 0,
                Nome = ObterString(item, "name") ?? "Competição",
                Codigo = ObterString(item, "code") ?? string.Empty,
                Tipo = ObterString(item, "type") ?? string.Empty,
                Plano = ObterString(item, "plan") ?? string.Empty,
                Pais = item.TryGetProperty("area", out var area)
                    ? ObterString(area, "name")
                    : null,
                EmblemaUrl = ObterString(item, "emblem"),
                TemporadaAtual = startDate?.Year,
                TemporadaAtualInicio = startDate,
                TemporadaAtualFim = endDate,
                RodadaAtual = currentSeason.ValueKind == JsonValueKind.Object
                    ? ObterInt(currentSeason, "currentMatchday")
                    : null
            };
        }

        private static FootballDataOrgTeamData ExtrairTime(JsonElement team)
        {
            return new FootballDataOrgTeamData
            {
                Id = ObterInt(team, "id") ?? 0,
                Nome = ObterString(team, "name") ?? string.Empty,
                NomeCurto = ObterString(team, "shortName"),
                Sigla = ObterString(team, "tla")
            };
        }

        private Jogo? LocalizarJogoParaVinculo(
            List<Jogo> jogosLocais,
            FootballDataOrgMatchData jogoApi,
            out string motivoIgnorado)
        {
            var candidatos = jogosLocais
                .Where(j =>
                    !j.FootballDataMatchId.HasValue &&
                    TimesCorrespondem(j.TimeCasa, jogoApi.TimeCasa) &&
                    TimesCorrespondem(j.TimeVisitante, jogoApi.TimeVisitante))
                .Select(j => new
                {
                    Jogo = j,
                    DistanciaDias = Math.Abs((
                        _appTimeService.NormalizarHorarioAplicacao(j.DataJogo).Date -
                        jogoApi.DataJogo.Date).TotalDays),
                    RodadaConfere = jogoApi.Matchday.HasValue && j.Rodada == jogoApi.Matchday
                })
                .Where(c => c.DistanciaDias <= 3 || c.RodadaConfere)
                .OrderByDescending(c => c.RodadaConfere)
                .ThenBy(c => c.DistanciaDias)
                .ToList();

            if (!candidatos.Any())
            {
                motivoIgnorado = "Sem FootballDataMatchId e sem jogo local compatível por times/data";
                return null;
            }

            var melhor = candidatos.First();
            var ambiguo = candidatos
                .Skip(1)
                .Any(c => c.RodadaConfere == melhor.RodadaConfere && c.DistanciaDias == melhor.DistanciaDias);

            if (ambiguo)
            {
                motivoIgnorado = "Vínculo ambíguo para FootballDataMatchId";
                return null;
            }

            motivoIgnorado = string.Empty;
            return melhor.Jogo;
        }

        private static bool TimesCorrespondem(Time? timeLocal, FootballDataOrgTeamData timeApi)
        {
            if (timeLocal == null)
            {
                return false;
            }

            var nomesLocais = new[]
            {
                timeLocal.Nome,
                timeLocal.Sigla
            }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(NormalizarTexto)
            .ToList();

            var nomesApi = new[]
            {
                timeApi.Nome,
                timeApi.NomeCurto,
                timeApi.Sigla
            }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(NormalizarTexto)
            .ToList();

            return nomesLocais.Any(local =>
                nomesApi.Any(api =>
                    local == api ||
                    local.Contains(api, StringComparison.OrdinalIgnoreCase) ||
                    api.Contains(local, StringComparison.OrdinalIgnoreCase)));
        }

        private bool AplicarDadosJogo(Jogo jogo, FootballDataOrgMatchData jogoApi)
        {
            var alterado = false;
            var dataJogoLocal = _appTimeService.NormalizarHorarioAplicacao(jogo.DataJogo);

            if (jogo.FootballDataMatchId != jogoApi.MatchId)
            {
                jogo.FootballDataMatchId = jogoApi.MatchId;
                alterado = true;
            }

            if (dataJogoLocal != jogoApi.DataJogo)
            {
                jogo.DataJogo = jogoApi.DataJogo;
                alterado = true;
            }

            if (!string.Equals(jogo.Status, jogoApi.Status, StringComparison.OrdinalIgnoreCase))
            {
                jogo.Status = jogoApi.Status;
                alterado = true;
            }

            if (jogoApi.Matchday.HasValue && jogo.Rodada != jogoApi.Matchday)
            {
                jogo.Rodada = jogoApi.Matchday;
                alterado = true;
            }

            if (!string.IsNullOrWhiteSpace(jogoApi.Stage) && !string.Equals(jogo.Fase, jogoApi.Stage, StringComparison.OrdinalIgnoreCase))
            {
                jogo.Fase = jogoApi.Stage;
                alterado = true;
            }

            if (!string.IsNullOrWhiteSpace(jogoApi.Grupo) && !string.Equals(jogo.Grupo, jogoApi.Grupo, StringComparison.OrdinalIgnoreCase))
            {
                jogo.Grupo = jogoApi.Grupo;
                alterado = true;
            }

            if (jogoApi.GolsCasa.HasValue && jogo.GolsCasa != jogoApi.GolsCasa)
            {
                jogo.GolsCasa = jogoApi.GolsCasa;
                alterado = true;
            }

            if (jogoApi.GolsVisitante.HasValue && jogo.GolsVisitante != jogoApi.GolsVisitante)
            {
                jogo.GolsVisitante = jogoApi.GolsVisitante;
                alterado = true;
            }

            return alterado;
        }

        private async Task RegistrarLogAsync(
            Campeonato campeonato,
            DateTime inicio,
            FootballDataOrgSyncResultado resultado,
            string? usuarioId,
            string? usuarioEmail,
            string? erroDetalhado = null)
        {
            await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
            {
                TipoSincronizacao = "FootballDataOrgResultados",
                CampeonatoId = campeonato.Id,
                FootballDataCompetitionId = campeonato.FootballDataCompetitionId,
                Temporada = resultado.Temporada,
                DataInicio = inicio,
                Status = resultado.Sucesso ? "Sucesso" : "Erro",
                TotalProcessados = resultado.JogosRetornadosApi,
                TotalCriados = resultado.JogosCriados,
                TotalAtualizados = resultado.JogosAtualizados,
                TotalIgnorados = resultado.JogosIgnorados,
                Mensagem = LimitarTexto(resultado.Mensagem, 500),
                ErroDetalhado = erroDetalhado ?? resultado.MontarResumoDetalhado(),
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail
            });
        }

        private static void AplicarConfiguracaoFootballData(
            Campeonato campeonato,
            int? footballDataCompetitionId,
            string? competitionCode,
            int? temporada)
        {
            if (footballDataCompetitionId.HasValue && footballDataCompetitionId.Value > 0)
            {
                campeonato.FootballDataCompetitionId = footballDataCompetitionId.Value;
            }

            var codigo = NormalizarCodigoCompeticao(competitionCode);

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                campeonato.FootballDataCompetitionCode = codigo;
            }

            if (temporada.HasValue && temporada.Value > 0)
            {
                campeonato.FootballDataSeason = temporada.Value;
            }
        }

        private static void AtualizarVinculoComCompeticaoValidada(
            Campeonato campeonato,
            FootballDataOrgCompetitionValidationResult validacao)
        {
            if (validacao.Id.HasValue && campeonato.FootballDataCompetitionId != validacao.Id)
            {
                campeonato.FootballDataCompetitionId = validacao.Id;
            }

            if (!string.IsNullOrWhiteSpace(validacao.Codigo) &&
                !string.Equals(campeonato.FootballDataCompetitionCode, validacao.Codigo, StringComparison.OrdinalIgnoreCase))
            {
                campeonato.FootballDataCompetitionCode = validacao.Codigo;
            }
        }

        private static string ObterIdentificadorCompeticao(Campeonato campeonato)
        {
            return !string.IsNullOrWhiteSpace(campeonato.FootballDataCompetitionCode)
                ? campeonato.FootballDataCompetitionCode.Trim().ToUpperInvariant()
                : campeonato.FootballDataCompetitionId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string MontarMensagemSincronizacao(FootballDataOrgSyncResultado resultado)
        {
            var mensagem =
                $"Resumo da sincronização {Fonte}. " +
                $"Campeonato local: {resultado.CampeonatoNome}; " +
                $"FootballDataCompetitionId usado: {resultado.FootballDataCompetitionId}; " +
                $"FootballDataCompetitionCode usado: {resultado.FootballDataCompetitionCode ?? "-"}; " +
                $"FootballDataSeason usada: {resultado.Temporada}; " +
                $"Endpoint chamado: {resultado.EndpointJogos ?? resultado.EndpointValidacao ?? "-"}; " +
                $"Status HTTP: {resultado.StatusHttpJogos?.ToString() ?? resultado.StatusHttpValidacao?.ToString() ?? "-"}; " +
                $"Jogos retornados: {resultado.JogosRetornadosApi}; " +
                $"criados: {resultado.JogosCriados}; atualizados: {resultado.JogosAtualizados}; " +
                $"ignorados: {resultado.JogosIgnorados}; erros: {resultado.Erros.Count}. ";

            mensagem += resultado.ClassificacaoRecalculada
                ? "Classificação e ranking dos bolões recalculados."
                : "Classificação não recalculada porque nenhum resultado foi alterado.";

            if (resultado.IgnoradosPorMotivo.Any())
            {
                var motivos = string.Join(
                    "; ",
                    resultado.IgnoradosPorMotivo.Select(m => $"{m.Key}: {m.Value}"));

                mensagem += $" Motivos dos ignorados: {motivos}.";
            }

            if (resultado.Avisos.Any())
            {
                mensagem += $" Avisos: {string.Join("; ", resultado.Avisos)}.";
            }

            if (resultado.Erros.Any())
            {
                mensagem += $" Erros: {string.Join("; ", resultado.Erros)}.";
            }

            return mensagem;
        }

        private static string ConverterStatus(string? statusApi)
        {
            return statusApi switch
            {
                "SCHEDULED" => "Agendado",
                "TIMED" => "Agendado",
                "IN_PLAY" => "Em andamento",
                "LIVE" => "Em andamento",
                "PAUSED" => "Em andamento",
                "FINISHED" => "Finalizado",
                "POSTPONED" => "Adiado",
                "CANCELLED" => "Cancelado",
                "CANCELED" => "Cancelado",
                "SUSPENDED" => "Suspenso",
                "AWARDED" => "Finalizado",
                _ => "Agendado"
            };
        }

        private static string MapearMensagemStatusHttp(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.OK => "Consulta realizada com sucesso.",
                HttpStatusCode.Unauthorized => "Chave football-data.org inválida ou não configurada.",
                HttpStatusCode.Forbidden => "Competição existe, mas não está disponível para sua chave/plano.",
                HttpStatusCode.NotFound => "Competição não encontrada no football-data.org.",
                HttpStatusCode.TooManyRequests => "Limite de chamadas atingido.",
                _ => $"Erro ao consultar {Fonte}. Status HTTP {(int)statusCode}."
            };
        }

        private string CriarEndpointResumo(string url)
        {
            return new Uri(_httpClient.BaseAddress!, url).ToString();
        }

        private static string NormalizarIdentificadorCompeticao(string? competitionIdentifier)
        {
            if (string.IsNullOrWhiteSpace(competitionIdentifier))
            {
                return string.Empty;
            }

            var identificador = competitionIdentifier.Trim();

            return identificador.All(char.IsDigit)
                ? identificador
                : identificador.ToUpperInvariant();
        }

        private static string NormalizarCodigoCompeticao(string? competitionCode)
        {
            return competitionCode?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static string NormalizarTexto(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var normalizado = texto.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalizado.Length);

            foreach (var caractere in normalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(caractere);

                if (categoria != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(caractere))
                {
                    builder.Append(char.ToUpperInvariant(caractere));
                }
            }

            var chave = builder.ToString();

            foreach (var sufixo in new[] { "FOOTBALLCLUB", "FC", "CF", "AC" })
            {
                if (chave.EndsWith(sufixo, StringComparison.OrdinalIgnoreCase) &&
                    chave.Length > sufixo.Length + 2)
                {
                    chave = chave[..^sufixo.Length];
                }
            }

            return chave.Normalize(NormalizationForm.FormC);
        }

        private static string LimitarTexto(string? texto, int maximo)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Length <= maximo)
            {
                return texto ?? string.Empty;
            }

            return texto.Substring(0, maximo);
        }

        private static string? ObterString(JsonElement element, string propriedade)
        {
            return element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propriedade, out var valor) &&
                valor.ValueKind != JsonValueKind.Null
                    ? valor.GetString()
                    : null;
        }

        private static int? ObterInt(JsonElement element, string propriedade)
        {
            return element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propriedade, out var valor) &&
                valor.ValueKind != JsonValueKind.Null
                    ? valor.GetInt32()
                    : null;
        }

        private static DateTime? ObterData(JsonElement element, string propriedade)
        {
            var texto = ObterString(element, propriedade);

            return DateTime.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var data)
                    ? data
                    : null;
        }

        private static List<int> ExtrairTemporadasDisponiveis(JsonElement competition)
        {
            if (!competition.TryGetProperty("seasons", out var seasons) ||
                seasons.ValueKind != JsonValueKind.Array)
            {
                return new List<int>();
            }

            return seasons
                .EnumerateArray()
                .Select(season => ObterData(season, "startDate")?.Year)
                .Where(ano => ano.HasValue)
                .Select(ano => ano!.Value)
                .Distinct()
                .OrderByDescending(ano => ano)
                .ToList();
        }
    }

    public class FootballDataOrgHttpException : Exception
    {
        public FootballDataOrgHttpException(
            HttpStatusCode statusCode,
            string endpoint,
            string userMessage)
            : base($"{userMessage} Endpoint: {endpoint}. Status HTTP: {(int)statusCode}.")
        {
            StatusCode = statusCode;
            Endpoint = endpoint;
            UserMessage = userMessage;
        }

        public HttpStatusCode StatusCode { get; }

        public string Endpoint { get; }

        public string UserMessage { get; }
    }

    public sealed class FootballDataOrgApiResponse : IDisposable
    {
        public FootballDataOrgApiResponse(
            JsonDocument documento,
            HttpStatusCode statusCode,
            string endpoint)
        {
            Documento = documento;
            StatusCode = statusCode;
            Endpoint = endpoint;
        }

        public JsonDocument Documento { get; }

        public HttpStatusCode StatusCode { get; }

        public string Endpoint { get; }

        public void Dispose()
        {
            Documento.Dispose();
        }
    }

    public class FootballDataOrgCompetitionValidationResult
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public string? Endpoint { get; set; }

        public int? StatusHttp { get; set; }

        public int? Id { get; set; }

        public string? Codigo { get; set; }

        public string? Nome { get; set; }

        public string? Plano { get; set; }

        public int? TemporadaAtual { get; set; }

        public List<int> TemporadasDisponiveis { get; set; } = new();

        public static FootballDataOrgCompetitionValidationResult Ok(
            string endpoint,
            int statusHttp,
            FootballDataOrgCompeticaoViewModel competicao)
        {
            return new FootballDataOrgCompetitionValidationResult
            {
                Sucesso = true,
                Endpoint = endpoint,
                StatusHttp = statusHttp,
                Id = competicao.Id,
                Codigo = string.IsNullOrWhiteSpace(competicao.Codigo) ? null : competicao.Codigo,
                Nome = competicao.Nome,
                Plano = competicao.Plano,
                TemporadaAtual = competicao.TemporadaAtual,
                Mensagem = $"Competição validada no football-data.org: {competicao.Nome} (ID {competicao.Id}, code {(string.IsNullOrWhiteSpace(competicao.Codigo) ? "-" : competicao.Codigo)}). Status HTTP {statusHttp}."
            };
        }

        public static FootballDataOrgCompetitionValidationResult Falha(
            string mensagem,
            string? endpoint,
            int? statusCode)
        {
            return new FootballDataOrgCompetitionValidationResult
            {
                Sucesso = false,
                Endpoint = endpoint,
                StatusHttp = statusCode,
                Mensagem = statusCode.HasValue
                    ? $"{mensagem} Status HTTP {statusCode.Value}."
                    : mensagem
            };
        }
    }

    public class FootballDataOrgMatchesResponse
    {
        public List<FootballDataOrgMatchData> Jogos { get; set; } = new();

        public string Endpoint { get; set; } = string.Empty;

        public int StatusHttp { get; set; }
    }

    public class FootballDataOrgMatchData
    {
        public int MatchId { get; set; }

        public DateTime DataJogo { get; set; }

        public string StatusApi { get; set; } = string.Empty;

        public string Status { get; set; } = "Agendado";

        public int? Matchday { get; set; }

        public string? Stage { get; set; }

        public string? Grupo { get; set; }

        public FootballDataOrgTeamData TimeCasa { get; set; } = new();

        public FootballDataOrgTeamData TimeVisitante { get; set; } = new();

        public int? GolsCasa { get; set; }

        public int? GolsVisitante { get; set; }

        public bool EstaFinalizado => string.Equals(Status, "Finalizado", StringComparison.OrdinalIgnoreCase);
    }

    public class FootballDataOrgTeamData
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? NomeCurto { get; set; }

        public string? Sigla { get; set; }
    }

    public class FootballDataOrgSyncResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public int CampeonatoId { get; set; }

        public string? CampeonatoNome { get; set; }

        public int? FootballDataCompetitionId { get; set; }

        public string? FootballDataCompetitionCode { get; set; }

        public int? CompeticaoFootballDataId { get; set; }

        public string? CompeticaoFootballDataCode { get; set; }

        public string? CompeticaoFootballDataNome { get; set; }

        public int? Temporada { get; set; }

        public string? EndpointValidacao { get; set; }

        public int? StatusHttpValidacao { get; set; }

        public string? EndpointJogos { get; set; }

        public int? StatusHttpJogos { get; set; }

        public int TotalProcessados { get; set; }

        public int JogosRetornadosApi { get; set; }

        public int JogosLocaisBanco { get; set; }

        public int JogosLocaisComFootballDataMatchId { get; set; }

        public int JogosEncontradosBanco { get; set; }

        public int JogosVinculadosFootballData { get; set; }

        public int JogosCriados { get; set; }

        public int JogosAtualizados { get; set; }

        public int JogosIgnorados { get; set; }

        public int JogosFinalizados { get; set; }

        public bool ClassificacaoRecalculada { get; set; }

        public bool RankingRecalculado { get; set; }

        public Dictionary<string, int> IgnoradosPorMotivo { get; set; } = new();

        public List<string> Avisos { get; set; } = new();

        public List<string> Erros { get; set; } = new();

        public void AdicionarIgnorado(string motivo)
        {
            motivo = string.IsNullOrWhiteSpace(motivo)
                ? "Motivo não informado"
                : motivo.Trim();

            JogosIgnorados++;

            if (!IgnoradosPorMotivo.TryAdd(motivo, 1))
            {
                IgnoradosPorMotivo[motivo]++;
            }
        }

        public string MontarResumoDetalhado()
        {
            var linhas = new List<string>
            {
                $"Campeonato local: {CampeonatoNome ?? CampeonatoId.ToString(CultureInfo.InvariantCulture)}",
                $"FootballDataCompetitionId usado: {FootballDataCompetitionId?.ToString(CultureInfo.InvariantCulture) ?? "-"}",
                $"FootballDataCompetitionCode usado: {FootballDataCompetitionCode ?? "-"}",
                $"Competição validada: {CompeticaoFootballDataNome ?? "-"} (ID {CompeticaoFootballDataId?.ToString(CultureInfo.InvariantCulture) ?? "-"}, code {CompeticaoFootballDataCode ?? "-"})",
                $"FootballDataSeason usada: {Temporada?.ToString(CultureInfo.InvariantCulture) ?? "-"}",
                $"Endpoint validação: {EndpointValidacao ?? "-"}",
                $"Status HTTP validação: {StatusHttpValidacao?.ToString(CultureInfo.InvariantCulture) ?? "-"}",
                $"Endpoint jogos: {EndpointJogos ?? "-"}",
                $"Status HTTP jogos: {StatusHttpJogos?.ToString(CultureInfo.InvariantCulture) ?? "-"}",
                "Filtros jogos: season=" + (Temporada?.ToString(CultureInfo.InvariantCulture) ?? "-"),
                $"Jogos retornados pela API: {JogosRetornadosApi}",
                $"Jogos locais no banco: {JogosLocaisBanco}",
                $"Jogos locais com FootballDataMatchId: {JogosLocaisComFootballDataMatchId}",
                $"Jogos encontrados no banco: {JogosEncontradosBanco}",
                $"Jogos vinculados ao FootballDataMatchId: {JogosVinculadosFootballData}",
                $"Jogos criados: {JogosCriados}",
                $"Jogos atualizados: {JogosAtualizados}",
                $"Jogos ignorados: {JogosIgnorados}",
                $"Jogos finalizados atualizados: {JogosFinalizados}",
                $"Classificação recalculada: {(ClassificacaoRecalculada ? "sim" : "não")}",
                $"Ranking recalculado: {(RankingRecalculado ? "sim" : "não")}"
            };

            if (IgnoradosPorMotivo.Any())
            {
                linhas.Add("Motivos dos ignorados:");

                foreach (var motivo in IgnoradosPorMotivo)
                {
                    linhas.Add($"- {motivo.Key}: {motivo.Value}");
                }
            }

            if (Avisos.Any())
            {
                linhas.Add("Avisos:");
                linhas.AddRange(Avisos.Select(aviso => $"- {aviso}"));
            }

            if (Erros.Any())
            {
                linhas.Add("Erros:");
                linhas.AddRange(Erros.Select(erro => $"- {erro}"));
            }

            return string.Join(Environment.NewLine, linhas);
        }

        public static FootballDataOrgSyncResultado Iniciar(int campeonatoId)
        {
            return new FootballDataOrgSyncResultado
            {
                Sucesso = true,
                CampeonatoId = campeonatoId
            };
        }

        public static FootballDataOrgSyncResultado Falha(string mensagem, int campeonatoId)
        {
            return new FootballDataOrgSyncResultado
            {
                Sucesso = false,
                CampeonatoId = campeonatoId,
                Mensagem = mensagem,
                Erros = new List<string> { mensagem }
            };
        }
    }
}
