using FutPlay.Data;
using FutPlay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
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

            return View(time);
        }

        [Authorize]
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

        [Authorize]
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