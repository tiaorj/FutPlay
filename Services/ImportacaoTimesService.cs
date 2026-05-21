using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class ImportacaoTimesService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;

        public ImportacaoTimesService(
            FootballApiService footballApiService,
            AppDbContext context)
        {
            _footballApiService = footballApiService;
            _context = context;
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesAsync(string pais)
        {
            using var resultado = await _footballApiService.BuscarTimesAsync(pais);

            return await MontarTimesAsync(resultado, pais);
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesPorNomeAsync(string nome)
        {
            using var resultado = await _footballApiService.BuscarTimesPorNomeAsync(nome);

            return await MontarTimesAsync(resultado);
        }

        public async Task<List<ApiTimeViewModel>> PesquisarTimesAsync(string termo)
        {
            using var resultado = await _footballApiService.PesquisarTimesAsync(termo);

            return await MontarTimesAsync(resultado);
        }

        public async Task<bool> ImportarTimeAsync(
            int apiTeamId,
            string nome,
            string? pais,
            string? escudoUrl)
        {
            bool jaExiste = await _context.Times
                .AnyAsync(t => t.ApiTeamId == apiTeamId || t.Nome == nome);

            if (jaExiste)
            {
                return false;
            }

            var time = new Time
            {
                Nome = nome,
                Sigla = GerarSigla(nome),
                Pais = pais,
                Tipo = "Clube",
                EscudoUrl = escudoUrl,
                Ativo = true,
                ApiTeamId = apiTeamId
            };

            _context.Times.Add(time);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesPorLigaAsync(int leagueId, int temporada)
        {
            using var resultado = await _footballApiService.BuscarTimesPorLigaAsync(leagueId, temporada);

            return await MontarTimesAsync(resultado);
        }

        private async Task<List<ApiTimeViewModel>> MontarTimesAsync(
            System.Text.Json.JsonDocument resultado,
            string? paisPadrao = null)
        {
            var times = new List<ApiTimeViewModel>();

            if (resultado.RootElement.TryGetProperty("response", out var response))
            {
                foreach (var item in response.EnumerateArray())
                {
                    var team = item.GetProperty("team");

                    int apiTeamId = team.GetProperty("id").GetInt32();
                    string nome = team.GetProperty("name").GetString() ?? string.Empty;

                    string? pais = team.TryGetProperty("country", out var countryElement)
                        ? countryElement.GetString()
                        : paisPadrao;

                    string? logo = team.TryGetProperty("logo", out var logoElement)
                        ? logoElement.GetString()
                        : null;

                    bool jaImportado = await _context.Times
                        .AnyAsync(t => t.ApiTeamId == apiTeamId || t.Nome == nome);

                    times.Add(new ApiTimeViewModel
                    {
                        ApiTeamId = apiTeamId,
                        Nome = nome,
                        Pais = pais,
                        EscudoUrl = logo,
                        JaImportado = jaImportado
                    });
                }
            }

            return times.OrderBy(t => t.Nome).ToList();
        }

        private static string GerarSigla(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return string.Empty;
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
    }
}
