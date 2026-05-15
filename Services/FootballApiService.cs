using FutPlay.Models.Api;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FutPlay.Services
{
    public class FootballApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiFootballOptions _options;
        private readonly ILogger<FootballApiService> _logger;

        public FootballApiService(HttpClient httpClient, IOptions<ApiFootballOptions> options, ILogger<FootballApiService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;

            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", _options.ApiKey);
            _logger = logger;
        }

        public async Task<JsonDocument> BuscarLigasAsync(string pais, int temporada)
        {
            var url = $"/leagues?country={Uri.EscapeDataString(pais)}&season={temporada}";

            _logger.LogInformation("Consultando API-Football: {Url}", url);
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogError("Erro ao consultar API-Football. StatusCode: {StatusCode}", response.StatusCode);
                throw new Exception("Limite diário da API atingido. Tente novamente mais tarde ou utilize uma temporada já importada.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao consultar API-Football. StatusCode: {StatusCode}", response.StatusCode);
                throw new Exception($"Erro ao consultar API de futebol: {(int)response.StatusCode} - {erro}");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json);
        }

        public async Task<JsonDocument> BuscarJogosAsync(int leagueId, int temporada)
        {
            var url = $"/fixtures?league={leagueId}&season={temporada}";

            _logger.LogInformation("Consultando API-Football: {Url}", url);
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogError("Erro ao consultar API-Football. StatusCode: {StatusCode}", response.StatusCode);
                throw new Exception("Limite diário da API atingido. Tente novamente mais tarde ou utilize uma temporada já importada.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao consultar API-Football. StatusCode: {StatusCode}", response.StatusCode);
                throw new Exception($"Erro ao consultar API de futebol: {(int)response.StatusCode} - {erro}");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json);
        }

        public async Task<JsonDocument> BuscarClassificacaoAsync(int leagueId, int temporada)
        {
            var url = $"/standings?league={leagueId}&season={temporada}";

            _logger.LogInformation("Consultando API-Football: {Url}", url);
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("Limite diário da API atingido. Tente novamente mais tarde ou utilize uma temporada já importada.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao consultar API-Football. StatusCode: {StatusCode}", response.StatusCode);
                throw new Exception($"Erro ao consultar API de futebol: {(int)response.StatusCode} - {erro}");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json);
        }
    }
}