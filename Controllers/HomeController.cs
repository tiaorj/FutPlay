using FutPlay.Data;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using FutPlay.Models;
using FutPlay.Services;

namespace FutPlay.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = new DashboardViewModel
            {
                TotalCampeonatos = await _context.Campeonatos.CountAsync(),
                TotalTimes = await _context.Times.CountAsync(),
                TotalJogos = await _context.Jogos.CountAsync(),
                TotalJogosAgendados = await _context.Jogos.CountAsync(j => j.Status == "Agendado"),
                TotalJogosFinalizados = await _context.Jogos.CountAsync(j => j.Status == "Finalizado"),
                TotalLigas = await _context.Ligas.CountAsync(),
                TotalParticipantes = await _context.LigaParticipantes.CountAsync(),
                TotalPalpites = await _context.Palpites.CountAsync(),

                CampeonatosAtivos = await _context.Campeonatos
                    .Where(c => c.Ativo)
                    .OrderByDescending(c => c.Ano)
                    .ThenBy(c => c.Nome)
                    .Take(6)
                    .ToListAsync(),

                ProximosJogos = await _context.Jogos
                    .Include(j => j.Campeonato)
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j =>
                        j.Ativo &&
                        j.DataJogo >= DateTime.Now &&
                        j.Status != "Finalizado")
                    .OrderBy(j => j.DataJogo)
                    .Take(6)
                    .ToListAsync(),

                UltimosResultados = await _context.Jogos
                    .Include(j => j.Campeonato)
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j =>
                        j.Ativo &&
                        j.Status == "Finalizado")
                    .OrderByDescending(j => j.DataJogo)
                    .Take(6)
                    .ToListAsync(),

                LigasPublicas = await _context.Ligas
                    .Include(l => l.Campeonato)
                    .Where(l => l.Ativo && l.Publica)
                    .OrderBy(l => l.Nome)
                    .Take(6)
                    .ToListAsync(),

                TopParticipantes = await _context.LigaParticipantes
                    .Include(p => p.Liga)
                    .Where(p => p.Ativo)
                    .OrderByDescending(p => p.PontuacaoTotal)
                    .ThenBy(p => p.Nome)
                    .Take(5)
                    .Select(p => new DashboardRankingViewModel
                    {
                        Nome = p.Nome,
                        Liga = p.Liga != null ? p.Liga.Nome : "",
                        Pontos = p.PontuacaoTotal
                    })
                    .ToListAsync()
            };

            return View(dashboard);
        }

        public async Task<IActionResult> Buscar(string? termo)
        {
            var termoNormalizado = termo?.Trim() ?? string.Empty;
            var viewModel = new BuscaViewModel
            {
                Termo = termoNormalizado,
                PesquisaExecutada = !string.IsNullOrWhiteSpace(termoNormalizado)
            };

            if (termoNormalizado.Length < 2)
            {
                return View(viewModel);
            }

            var like = $"%{termoNormalizado}%";
            var pesquisaAno = int.TryParse(termoNormalizado, out var anoBusca);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usuarioAutenticado = User.Identity?.IsAuthenticated == true;
            var usuarioAdmin = User.IsInRole(AppRoles.Administrador);

            viewModel.Jogos = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.Ativo &&
                    ((j.TimeCasa != null && EF.Functions.Like(j.TimeCasa.Nome, like)) ||
                     (j.TimeCasa != null && j.TimeCasa.Sigla != null && EF.Functions.Like(j.TimeCasa.Sigla, like)) ||
                     (j.TimeVisitante != null && EF.Functions.Like(j.TimeVisitante.Nome, like)) ||
                     (j.TimeVisitante != null && j.TimeVisitante.Sigla != null && EF.Functions.Like(j.TimeVisitante.Sigla, like)) ||
                     (j.Campeonato != null && EF.Functions.Like(j.Campeonato.Nome, like)) ||
                     (j.Fase != null && EF.Functions.Like(j.Fase, like)) ||
                     (j.Grupo != null && EF.Functions.Like(j.Grupo, like)) ||
                     EF.Functions.Like(j.Status, like)))
                .OrderBy(j => j.DataJogo)
                .Take(8)
                .ToListAsync();

            var ligasQuery = _context.Ligas
                .Include(l => l.Campeonato)
                .Where(l => l.Ativo);

            if (!usuarioAdmin)
            {
                if (usuarioAutenticado && !string.IsNullOrWhiteSpace(userId))
                {
                    ligasQuery = ligasQuery.Where(l =>
                        l.Publica ||
                        l.CriadorUserId == userId ||
                        _context.LigaParticipantes.Any(p =>
                            p.LigaId == l.Id &&
                            p.Ativo &&
                            p.UserId == userId));
                }
                else
                {
                    ligasQuery = ligasQuery.Where(l => l.Publica);
                }
            }

            viewModel.Ligas = await ligasQuery
                .Where(l =>
                    EF.Functions.Like(l.Nome, like) ||
                    EF.Functions.Like(l.CodigoConvite, like) ||
                    (l.Campeonato != null && EF.Functions.Like(l.Campeonato.Nome, like)))
                .OrderBy(l => l.Nome)
                .Take(8)
                .ToListAsync();

            viewModel.Times = await _context.Times
                .Where(t =>
                    t.Ativo &&
                    (EF.Functions.Like(t.Nome, like) ||
                     (t.Sigla != null && EF.Functions.Like(t.Sigla, like)) ||
                     (t.Pais != null && EF.Functions.Like(t.Pais, like)) ||
                     EF.Functions.Like(t.Tipo, like)))
                .OrderBy(t => t.Nome)
                .Take(8)
                .ToListAsync();

            viewModel.Campeonatos = await _context.Campeonatos
                .Where(c =>
                    c.Ativo &&
                    (EF.Functions.Like(c.Nome, like) ||
                     (c.Pais != null && EF.Functions.Like(c.Pais, like)) ||
                     (c.Tipo != null && EF.Functions.Like(c.Tipo, like)) ||
                     (pesquisaAno && c.Ano == anoBusca)))
                .OrderByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .Take(8)
                .ToListAsync();

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
