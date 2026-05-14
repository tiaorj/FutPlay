using FutPlay.Data;
using FutPlay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class JogosController : Controller
    {
        private readonly AppDbContext _context;

        public JogosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var jogos = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            return View(jogos);
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            await CarregarCombos();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Jogo jogo)
        {
            if (jogo.TimeCasaId == jogo.TimeVisitanteId)
            {
                ModelState.AddModelError("", "O time da casa e o time visitante não podem ser iguais.");
            }

            if (ModelState.IsValid)
            {
                _context.Jogos.Add(jogo);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(jogo);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var jogo = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jogo == null)
                return NotFound();

            return View(jogo);
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var jogo = await _context.Jogos.FindAsync(id);

            if (jogo == null)
                return NotFound();

            await CarregarCombos();
            return View(jogo);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Jogo jogo)
        {
            if (id != jogo.Id)
                return NotFound();

            if (jogo.TimeCasaId == jogo.TimeVisitanteId)
            {
                ModelState.AddModelError("", "O time da casa e o time visitante não podem ser iguais.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(jogo);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(jogo);
        }

        private async Task CarregarCombos()
        {
            ViewBag.CampeonatoId = new SelectList(
                await _context.Campeonatos
                    .Where(c => c.Ativo)
                    .OrderByDescending(c => c.Ano)
                    .ThenBy(c => c.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );

            ViewBag.TimeCasaId = new SelectList(
                await _context.Times
                    .Where(t => t.Ativo)
                    .OrderBy(t => t.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );

            ViewBag.TimeVisitanteId = new SelectList(
                await _context.Times
                    .Where(t => t.Ativo)
                    .OrderBy(t => t.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );
        }
    }
}