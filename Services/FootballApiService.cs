using FutPlay.Models.Api;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FutPlay.Services
{
    public class FootballApiService
    {
        private const string LimiteApiMensagem = "Limite de requisições da API atingido. Tente novamente mais tarde.";
        private const string ErroApiMensagem = "Erro ao consultar API de futebol. Tente novamente mais tarde.";

        private readonly HttpClient _httpClient;
        private readonly ApiFootballOptions _options;
        private readonly ILogger<FootballApiService> _logger;

        public FootballApiService(HttpClient httpClient, IOptions<ApiFootballOptions> options, ILogger<FootballApiService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;

            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-apisports-key", _options.ApiKey);
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

        public async Task<JsonDocument> BuscarTimesPorLigaAsync(int leagueId, int temporada)
        {
            var url = $"/teams?league={leagueId}&season={temporada}";

            return await ConsultarApiAsync(url, "times", leagueId);
        }

        private async Task<JsonDocument> ConsultarApiAsync(string url, string recurso, int? apiLeagueId = null)
        {
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

                throw new Exception(ErroApiMensagem);
            }

            using (response)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning(
                        "Limite de requisicoes da API-Football atingido. StatusCode: {StatusCode}. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                        response.StatusCode,
                        recurso,
                        apiLeagueId);

                    throw new Exception(LimiteApiMensagem);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro HTTP ao consultar API-Football. StatusCode: {StatusCode}. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                        response.StatusCode,
                        recurso,
                        apiLeagueId);

                    throw new Exception(ErroApiMensagem);
                }

                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Consulta API-Football concluida com sucesso. Recurso: {Recurso}. ApiLeagueId: {ApiLeagueId}",
                    recurso,
                    apiLeagueId);

                return JsonDocument.Parse(json);
            }
        }
    }
}
