using FutPlay.Data;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using FutPlay.Models;

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

                ProximosJogos = await _context.Jogos
                    .Include(j => j.Campeonato)
                    .Include(j => j.TimeCasa)
                    .Include(j => j.TimeVisitante)
                    .Where(j => j.Ativo && j.DataJogo >= DateTime.Now)
                    .OrderBy(j => j.DataJogo)
                    .Take(5)
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