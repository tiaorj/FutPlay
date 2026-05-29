using FutPlay.Models.Api;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace FutPlay.Services
{
    public class FootballApiService
    {
        private const string BaseUrlPadrao = "https://v3.football.api-sports.io";
        private const string LimiteApiMensagem = "Limite de requisições da API atingido. Tente novamente mais tarde.";
        private const string ErroApiMensagem = "Erro ao consultar API de futebol. Tente novamente mais tarde.";
        private const string ApiKeyAusenteMensagem = "Configure ApiFootball:ApiKey antes de consultar a API-Football.";

        private readonly HttpClient _httpClient;
        private readonly ApiFootballOptions _options;
        private readonly ILogger<FootballApiService> _logger;

        public FootballApiService(HttpClient httpClient, IOptions<ApiFootballOptions> options, ILogger<FootballApiService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;

            _httpClient.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? BaseUrlPadrao
                    : _options.BaseUrl.TrimEnd('/'));

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-apisports-key", _options.ApiKey);
            }

            _logger = logger;
        }

        public async Task<JsonDocument> BuscarLigasAsync(string pais, int temporada)
        {
            var url = $"/leagues?country={Uri.EscapeDataString(pais)}&season={temporada}";

            return await ConsultarApiAsync(url, "ligas");
        }

        public async Task<JsonDocument> BuscarJogosAsync(int leagueId, int temporada)
        {
            var url = $"/fixtures?league={leagueId}&season={temporada}";

            return await ConsultarApiAsync(url, "jogos", leagueId);
        }

        public async Task<JsonDocument> BuscarClassificacaoAsync(int leagueId, int temporada)
        {
            var url = $"/standings?league={leagueId}&season={temporada}";

            return await ConsultarApiAsync(url, "classificacao", leagueId);
        }

        public async Task<JsonDocument> BuscarTimesAsync(string pais)
        {
            var url = $"/teams?country={Uri.EscapeDataString(pais)}";

            return await ConsultarApiAsync(url, "times");
        }

        public async Task<JsonDocument> BuscarTimesPorNomeAsync(string nome)
        {
            var url = $"/teams?name={Uri.EscapeDataString(nome)}";

            return await ConsultarApiAsync(url, "times");
        }

        public async Task<JsonDocument> BuscarTimesPorPaisENomeAsync(string pais, string nome)
        {
            var url = $"/teams?country={Uri.EscapeDataString(pais)}&name={Uri.EscapeDataString(nome)}";

            return await ConsultarApiAsync(url, "times");
        }

        public async Task<JsonDocument> PesquisarTimesAsync(string termo)
        {
            var url = $"/teams?search={Uri.EscapeDataString(termo)}";

            return await ConsultarApiAsync(url, "times");
        }

        public async Task<JsonDocument> BuscarTimesPorLigaAsync(int leagueId, int temporada)
        {
            var url = $"/teams?league={leagueId}&season={temporada}";

            return await ConsultarApiAsync(url, "times", leagueId);
        }

        public async Task<ApiFootballStatusResultado> VerificarStatusAsync()
        {
            using var resultado = await ConsultarApiAsync("/status", "status");

            if (!resultado.RootElement.TryGetProperty("response", out var response) ||
                response.ValueKind != JsonValueKind.Object)
            {
                return ApiFootballStatusResultado.Ok("API-Football respondeu, mas o status retornou em um formato inesperado.");
            }

            response.TryGetProperty("requests", out var requests);
            response.TryGetProperty("subscription", out var subscription);

            var requisicoesHoje = ObterInt(requests, "current");
            var limiteDiario = ObterInt(requests, "limit_day");
            var plano = ObterString(subscription, "plan");

            var detalhes = new List<string>();

            if (!string.IsNullOrWhiteSpace(plano))
            {
                detalhes.Add($"plano {plano}");
            }

            if (requisicoesHoje.HasValue && limiteDiario.HasValue)
            {
                detalhes.Add($"{requisicoesHoje}/{limiteDiario} requisições usadas hoje");
            }
            else if (requisicoesHoje.HasValue)
            {
                detalhes.Add($"{requisicoesHoje} requisições usadas hoje");
            }

            var sufixo = detalhes.Any()
                ? $" ({string.Join("; ", detalhes)})"
                : string.Empty;

            return ApiFootballStatusResultado.Ok($"API-Football online e autenticada{sufixo}.");
        }

        private async Task<JsonDocument> ConsultarApiAsync(string url, string recurso, int? apiLeagueId = null)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(ApiKeyAusenteMensagem);
            }

            _logger.LogInformation(
                "Iniciando consulta API-Football. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                recurso,
                apiLeagueId);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Erro ao consultar API-Football. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                    recurso,
                    apiLeagueId);

                throw new InvalidOperationException(ErroApiMensagem, ex);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning(
                        "Limite de requisicoes da API-Football atingido. StatusCode: {StatusCode}. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                        response.StatusCode,
                        recurso,
                        apiLeagueId);

                    throw new InvalidOperationException(LimiteApiMensagem);
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning(
                        "Chave da API-Football invalida ou sem permissao. StatusCode: {StatusCode}. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                        response.StatusCode,
                        recurso,
                        apiLeagueId);

                    throw new InvalidOperationException("Chave da API-Football inválida, ausente ou sem permissão para este recurso.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro HTTP ao consultar API-Football. StatusCode: {StatusCode}. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                        response.StatusCode,
                        recurso,
                        apiLeagueId);

                    throw new InvalidOperationException(ErroApiMensagem);
                }

                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Consulta API-Football concluida com sucesso. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                    recurso,
                    apiLeagueId);

                try
                {
                    return JsonDocument.Parse(json);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Resposta JSON invalida da API-Football. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                        recurso,
                        apiLeagueId);

                    throw new InvalidOperationException("A API-Football respondeu, mas o JSON retornado não pôde ser lido.", ex);
                }
            }
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
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propriedade, out var valor) ||
                valor.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (valor.ValueKind == JsonValueKind.Number && valor.TryGetInt32(out var numero))
            {
                return numero;
            }

            if (valor.ValueKind == JsonValueKind.String && int.TryParse(valor.GetString(), out numero))
            {
                return numero;
            }

            return null;
        }
    }

    public class ApiFootballStatusResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public static ApiFootballStatusResultado Ok(string mensagem)
        {
            return new ApiFootballStatusResultado
            {
                Sucesso = true,
                Mensagem = mensagem
            };
        }
    }
}
