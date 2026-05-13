using FutPlay.Data;
using FutPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class LigasController : Controller
    {
        private readonly AppDbContext _context;

        public LigasController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ligas = await _context.Ligas
                .Include(l => l.Campeonato)
                .OrderByDescending(l => l.Ativo)
                .ThenBy(l => l.Nome)
                .ToListAsync();

            return View(ligas);
        }

        public async Task<IActionResult> Create()
        {
            await CarregarCampeonatos();
            return View(new Liga
            {
                CodigoConvite = GerarCodigoConvite(),
                DataCriacao = DateTime.Now,
                Ativo = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Liga liga)
        {
            if (string.IsNullOrWhiteSpace(liga.CodigoConvite))
            {
                liga.CodigoConvite = GerarCodigoConvite();
            }

            liga.DataCriacao = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Ligas.Add(liga);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarCampeonatos();
            return View(liga);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var liga = await _context.Ligas
                .Include(l => l.Campeonato)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (liga == null)
                return NotFound();

            return View(liga);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var liga = await _context.Ligas.FindAsync(id);

            if (liga == null)
                return NotFound();

            await CarregarCampeonatos();
            return View(liga);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Liga liga)
        {
            if (id != liga.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(liga);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarCampeonatos();
            return View(liga);
        }

        private async Task CarregarCampeonatos()
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
        }

        private string GerarCodigoConvite()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }
    }
}