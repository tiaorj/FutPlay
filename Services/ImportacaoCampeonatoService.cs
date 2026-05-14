using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class ImportacaoCampeonatoService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;

        public ImportacaoCampeonatoService(
            FootballApiService footballApiService,
            AppDbContext context)
        {
            _footballApiService = footballApiService;
            _context = context;
        }

        public async Task<List<ApiLigaViewModel>> BuscarLigasAsync(string pais, int temporada)
        {
            var ligas = new List<ApiLigaViewModel>();

            using var resultado = await _footballApiService.BuscarLigasAsync(pais, temporada);

            if (resultado.RootElement.TryGetProperty("response", out var response))
            {
                foreach (var item in response.EnumerateArray())
                {
                    var league = item.GetProperty("league");
                    var country = item.GetProperty("country");

                    int apiLeagueId = league.GetProperty("id").GetInt32();
                    string nome = league.GetProperty("name").GetString() ?? "";
                    string tipo = league.GetProperty("type").GetString() ?? "";
                    string paisNome = country.GetProperty("name").GetString() ?? "";
                    string? logo = league.TryGetProperty("logo", out var logoElement)
                        ? logoElement.GetString()
                        : null;

                    bool jaImportado = await _context.Campeonatos
                        .AnyAsync(c => c.ApiLeagueId == apiLeagueId && c.Ano == temporada);

                    ligas.Add(new ApiLigaViewModel
                    {
                        ApiLeagueId = apiLeagueId,
                        Nome = nome,
                        Tipo = tipo,
                        Pais = paisNome,
                        Temporada = temporada,
                        LogoUrl = logo,
                        JaImportado = jaImportado
                    });
                }
            }

            return ligas;
        }

        public async Task<bool> ImportarLigaAsync(
            int apiLeagueId,
            string nome,
            string tipo,
            string pais,
            string? logoUrl,
            int temporada)
        {
            var jaExiste = await _context.Campeonatos
                .AnyAsync(c => c.ApiLeagueId == apiLeagueId && c.Ano == temporada);

            if (jaExiste)
            {
                return false;
            }

            var campeonato = new Campeonato
            {
                Nome = nome,
                Ano = temporada,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "Liga" : tipo,
                Pais = pais,
                LogoUrl = logoUrl,
                Ativo = true,
                ApiLeagueId = apiLeagueId
            };

            _context.Campeonatos.Add(campeonato);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
