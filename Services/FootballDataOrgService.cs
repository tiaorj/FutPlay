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

            _httpClient.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://api.football-data.org/v4"
                    : _options.BaseUrl.TrimEnd('/'));

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Auth-Token", _options.ApiKey);
            }
        }

        public async Task<List<FootballDataOrgCompeticaoViewModel>> ListarCompeticoesAsync()
        {
            using var documento = await ConsultarApiAsync("/competitions", "competições");
            var competicoes = new List<FootballDataOrgCompeticaoViewModel>();

            if (!documento.RootElement.TryGetProperty("competitions", out var lista))
            {
                return competicoes;
            }

            foreach (var item in lista.EnumerateArray())
            {
                competicoes.Add(new FootballDataOrgCompeticaoViewModel
                {
                    Id = ObterInt(item, "id") ?? 0,
                    Nome = ObterString(item, "name") ?? "Competição",
                    Codigo = ObterString(item, "code") ?? string.Empty,
                    Tipo = ObterString(item, "type") ?? string.Empty,
                    Pais = item.TryGetProperty("area", out var area)
                        ? ObterString(area, "name")
                        : null,
                    EmblemaUrl = ObterString(item, "emblem"),
                    TemporadaAtual = item.TryGetProperty("currentSeason", out var temporada) &&
                        temporada.ValueKind == JsonValueKind.Object &&
                        temporada.TryGetProperty("startDate", out var startDate) &&
                        DateTime.TryParse(startDate.GetString(), out var inicio)
                            ? inicio.Year
                            : null
                });
            }

            return competicoes
                .OrderBy(c => c.Pais)
                .ThenBy(c => c.Nome)
                .ToList();
        }

        public async Task<List<FootballDataOrgMatchData>> BuscarJogosCompeticaoAsync(
            string competitionCode,
            int temporada)
        {
            var codigo = NormalizarCodigoCompeticao(competitionCode);
            using var documento = await ConsultarApiAsync(
                $"/competitions/{Uri.EscapeDataString(codigo)}/matches?season={temporada}",
                $"jogos {codigo}/{temporada}");

            var jogos = new List<FootballDataOrgMatchData>();

            if (!documento.RootElement.TryGetProperty("matches", out var matches))
            {
                return jogos;
            }

            foreach (var item in matches.EnumerateArray())
            {
                jogos.Add(ExtrairJogo(item));
            }

            return jogos;
        }

        public async Task<FootballDataOrgSyncResultado> AtualizarResultadosAsync(
            int campeonatoId,
            string competitionCode,
            int temporada,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            var inicio = DateTime.UtcNow;
            var codigo = NormalizarCodigoCompeticao(competitionCode);
            var resultado = FootballDataOrgSyncResultado.Iniciar(campeonatoId);

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                return FootballDataOrgSyncResultado.Falha("Campeonato não encontrado.", campeonatoId);
            }

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return FootballDataOrgSyncResultado.Falha("Informe o código da competição no football-data.org.", campeonatoId);
            }

            try
            {
                var jogosApi = await BuscarJogosCompeticaoAsync(codigo, temporada);
                resultado.TotalProcessados = jogosApi.Count;

                if (!jogosApi.Any())
                {
                    resultado.Sucesso = false;
                    resultado.Mensagem = $"Nenhum jogo encontrado no {Fonte} para {codigo}/{temporada}. Mantenha atualização manual ou importação por planilha.";
                    resultado.Erros.Add(resultado.Mensagem);

                    await RegistrarLogAsync(campeonato, inicio, codigo, temporada, resultado, usuarioId, usuarioEmail);
                    return resultado;
                }

                var jogosLocais = await _context.Jogos
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j => j.CampeonatoId == campeonato.Id && j.Ativo)
                    .ToListAsync();

                foreach (var jogoApi in jogosApi)
                {
                    var jogoLocal = LocalizarJogo(jogosLocais, jogoApi);

                    if (jogoLocal == null)
                    {
                        resultado.JogosIgnorados++;
                        continue;
                    }

                    if (!AplicarDadosJogo(jogoLocal, jogoApi))
                    {
                        resultado.JogosIgnorados++;
                        continue;
                    }

                    _context.Jogos.Update(jogoLocal);
                    resultado.JogosAtualizados++;

                    if (jogoApi.EstaFinalizado)
                    {
                        resultado.JogosFinalizados++;
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

                resultado.Sucesso = true;
                resultado.Mensagem =
                    $"Sincronização {Fonte} concluída para {codigo}/{temporada}. " +
                    $"Jogos atualizados: {resultado.JogosAtualizados}; ignorados: {resultado.JogosIgnorados}; finalizados: {resultado.JogosFinalizados}. " +
                    (resultado.ClassificacaoRecalculada
                        ? "Classificação e ranking dos bolões recalculados."
                        : "Nenhum jogo existente foi alterado; fluxo manual segue disponível.");

                await RegistrarLogAsync(campeonato, inicio, codigo, temporada, resultado, usuarioId, usuarioEmail);

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao sincronizar resultados via football-data.org. CampeonatoId: {CampeonatoId}. Codigo: {Codigo}. Temporada: {Temporada}",
                    campeonato.Id,
                    codigo,
                    temporada);

                resultado.Sucesso = false;
                resultado.Mensagem = $"Erro ao sincronizar via {Fonte}: {ex.Message}";
                resultado.Erros.Add(ex.Message);

                await RegistrarLogAsync(campeonato, inicio, codigo, temporada, resultado, usuarioId, usuarioEmail, ex.ToString());

                return resultado;
            }
        }

        private async Task<JsonDocument> ConsultarApiAsync(string url, string recurso)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Configure FootballDataOrg:ApiKey antes de consultar o football-data.org.");
            }

            using var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Competição ou recurso não encontrado no {Fonte}.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException($"Limite de requisições do {Fonte} atingido. Tente novamente mais tarde.");
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException($"Chave do {Fonte} inválida, ausente ou sem permissão para este recurso.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Erro ao consultar {Fonte}. Recurso: {recurso}. Status: {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json);
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

            return new FootballDataOrgMatchData
            {
                MatchId = ObterInt(item, "id") ?? 0,
                DataJogo = _appTimeService.ConverterUtcParaHorarioAplicacao(utcDate),
                StatusApi = ObterString(item, "status") ?? string.Empty,
                Status = ConverterStatus(ObterString(item, "status")),
                Matchday = ObterInt(item, "matchday"),
                Stage = ObterString(item, "stage"),
                Grupo = ObterString(item, "group"),
                TimeCasa = ExtrairTime(homeTeam),
                TimeVisitante = ExtrairTime(awayTeam),
                GolsCasa = ObterInt(fullTime, "home"),
                GolsVisitante = ObterInt(fullTime, "away")
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

        private Jogo? LocalizarJogo(
            List<Jogo> jogosLocais,
            FootballDataOrgMatchData jogoApi)
        {
            var candidatos = jogosLocais
                .Where(j =>
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

            return candidatos.FirstOrDefault()?.Jogo;
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
            string codigo,
            int temporada,
            FootballDataOrgSyncResultado resultado,
            string? usuarioId,
            string? usuarioEmail,
            string? erroDetalhado = null)
        {
            await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
            {
                TipoSincronizacao = "FootballDataOrgResultados",
                CampeonatoId = campeonato.Id,
                Temporada = temporada,
                DataInicio = inicio,
                Status = resultado.Sucesso ? "Sucesso" : "Erro",
                TotalProcessados = resultado.TotalProcessados,
                TotalAtualizados = resultado.JogosAtualizados,
                TotalIgnorados = resultado.JogosIgnorados,
                Mensagem = $"[{codigo}] {resultado.Mensagem}",
                ErroDetalhado = erroDetalhado ?? (resultado.Erros.Any() ? string.Join(Environment.NewLine, resultado.Erros) : null),
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail
            });
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
                "SUSPENDED" => "Suspenso",
                "CANCELLED" => "Cancelado",
                "CANCELED" => "Cancelado",
                "AWARDED" => "Finalizado",
                _ => "Agendado"
            };
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

        private static string? ObterString(JsonElement element, string propriedade)
        {
            return element.TryGetProperty(propriedade, out var valor) && valor.ValueKind != JsonValueKind.Null
                ? valor.GetString()
                : null;
        }

        private static int? ObterInt(JsonElement element, string propriedade)
        {
            return element.TryGetProperty(propriedade, out var valor) && valor.ValueKind != JsonValueKind.Null
                ? valor.GetInt32()
                : null;
        }
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

        public int TotalProcessados { get; set; }

        public int JogosAtualizados { get; set; }

        public int JogosIgnorados { get; set; }

        public int JogosFinalizados { get; set; }

        public bool ClassificacaoRecalculada { get; set; }

        public bool RankingRecalculado { get; set; }

        public List<string> Erros { get; set; } = new();

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
