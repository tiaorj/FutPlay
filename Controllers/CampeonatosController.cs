using FutPlay.Data;
using FutPlay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutPlay.ViewModels;

namespace FutPlay.Controllers
{
    public class CampeonatosController : Controller
    {
        private readonly AppDbContext _context;

        public CampeonatosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var campeonatos = await _context.Campeonatos
                .OrderByDescending(c => c.Ativo)
                .OrderByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            return View(campeonatos);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Campeonato campeonato)
        {
            if (ModelState.IsValid)
            {
                _context.Campeonatos.Add(campeonato);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(campeonato);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campeonato == null)
            {
                return NotFound();
            }

            return View(campeonato);
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campeonato = await _context.Campeonatos.FindAsync(id);

            if (campeonato == null)
            {
                return NotFound();
            }

            return View(campeonato);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Campeonato campeonato)
        {
            if (id != campeonato.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(campeonato);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    var existe = await _context.Campeonatos.AnyAsync(c => c.Id == campeonato.Id);

                    if (!existe)
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(campeonato);
        }

        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campeonato == null)
            {
                return NotFound();
            }

            return View(campeonato);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var campeonato = await _context.Campeonatos.FindAsync(id);

            if (campeonato == null)
            {
                return NotFound();
            }

            campeonato.Ativo = false;

            _context.Campeonatos.Update(campeonato);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Classificacao(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campeonato == null)
            {
                return NotFound();
            }

            var classificacoes = await _context.Classificacoes
                .Include(c => c.Time)
                .Where(c => c.CampeonatoId == id && c.Ativo)
                .OrderBy(c => c.Grupo)
                .ThenBy(c => c.Posicao)
                .ToListAsync();

            var viewModel = new ClassificacaoCampeonatoViewModel
            {
                Campeonato = campeonato,
                Classificacoes = classificacoes
            };

            return View(viewModel);
        }

    }
}