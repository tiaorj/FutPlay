using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutPlay.ViewModels;

namespace FutPlay.Controllers
{
    public class TimesController : Controller
    {
        private readonly AppDbContext _context;

        public TimesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var times = await _context.Times
                .OrderByDescending(t => t.Ativo)
                .ThenBy(t => t.Tipo)
                .ThenBy(t => t.Nome)
                .ToListAsync();

            return View(times);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = AppRoles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Time time)
        {
            if (ModelState.IsValid)
            {
                _context.Times.Add(time);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(time);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var time = await _context.Times
                .FirstOrDefaultAsync(t => t.Id == id);

            if (time == null)
            {
                return NotFound();
            }

            var jogosDoTime = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.Ativo &&
                    (j.TimeCasaId == time.Id || j.TimeVisitanteId == time.Id))
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            var proximosJogos = jogosDoTime
                .Where(j =>
                    j.DataJogo >= DateTime.Now &&
                    !string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                .OrderBy(j => j.DataJogo)
                .Take(6)
                .ToList();

            var ultimosResultados = jogosDoTime
                .Where(j =>
                    string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(j => j.DataJogo)
                .Take(6)
                .ToList();

            var campeonatos = jogosDoTime
                .Where(j => j.Campeonato != null)
                .Select(j => j.Campeonato!)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .ToList();

            int totalVitorias = 0;
            int totalEmpates = 0;
            int totalDerrotas = 0;
            int golsPro = 0;
            int golsContra = 0;

            foreach (var jogo in jogosDoTime.Where(j =>
                string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                j.GolsCasa.HasValue &&
                j.GolsVisitante.HasValue))
            {
                bool timeCasa = jogo.TimeCasaId == time.Id;

                int golsTime = timeCasa ? jogo.GolsCasa!.Value : jogo.GolsVisitante!.Value;
                int golsAdversario = timeCasa ? jogo.GolsVisitante!.Value : jogo.GolsCasa!.Value;

                golsPro += golsTime;
                golsContra += golsAdversario;

                if (golsTime > golsAdversario)
                {
                    totalVitorias++;
                }
                else if (golsTime < golsAdversario)
                {
                    totalDerrotas++;
                }
                else
                {
                    totalEmpates++;
                }
            }

            var viewModel = new TimeDetalhesViewModel
            {
                Time = time,
                ProximosJogos = proximosJogos,
                UltimosResultados = ultimosResultados,
                Campeonatos = campeonatos,
                TotalJogos = jogosDoTime.Count,
                TotalVitorias = totalVitorias,
                TotalEmpates = totalEmpates,
                TotalDerrotas = totalDerrotas,
                GolsPro = golsPro,
                GolsContra = golsContra
            };

            return View(viewModel);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var time = await _context.Times.FindAsync(id);

            if (time == null)
            {
                return NotFound();
            }

            return View(time);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Time time)
        {
            if (id != time.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(time);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    var existe = await _context.Times.AnyAsync(t => t.Id == time.Id);

                    if (!existe)
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(time);
        }
    }
}
