using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class CampeonatosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ClassificacaoService _classificacaoService;

        public CampeonatosController(
            AppDbContext context,
            ClassificacaoService classificacaoService)
        {
            _context = context;
            _classificacaoService = classificacaoService;
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
            const int anoMin = 1900;
            int anoMax = DateTime.Now.Year + 5;

            if (campeonato.Ano < anoMin || campeonato.Ano > anoMax)
            {
                ModelState.AddModelError("Ano", $"O ano deve estar entre {anoMin} e {anoMax}.");
            }

            if (campeonato.DataInicio.HasValue && campeonato.DataFim.HasValue &&
                campeonato.DataFim.Value < campeonato.DataInicio.Value)
            {
                ModelState.AddModelError("DataFim", "A data de fim não pode ser anterior à data de início.");
            }

            bool existeDuplicado = await _context.Campeonatos
                .AnyAsync(c => c.Nome == campeonato.Nome && c.Ano == campeonato.Ano);

            if (existeDuplicado)
            {
                ModelState.AddModelError("Nome", "Já existe um campeonato com esse nome e ano.");
            }

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

            const int anoMin = 1900;
            int anoMax = DateTime.Now.Year + 5;

            if (campeonato.Ano < anoMin || campeonato.Ano > anoMax)
            {
                ModelState.AddModelError("Ano", $"O ano deve estar entre {anoMin} e {anoMax}.");
            }

            if (campeonato.DataInicio.HasValue && campeonato.DataFim.HasValue &&
                campeonato.DataFim.Value < campeonato.DataInicio.Value)
            {
                ModelState.AddModelError("DataFim", "A data de fim não pode ser anterior à data de início.");
            }

            bool existeDuplicado = await _context.Campeonatos
                .AnyAsync(c => c.Id != campeonato.Id && c.Nome == campeonato.Nome && c.Ano == campeonato.Ano);

            if (existeDuplicado)
            {
                ModelState.AddModelError("Nome", "Já existe outro campeonato com esse nome e ano.");
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

        [Authorize]
        public async Task<IActionResult> RecalcularClassificacao(int? id)
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

            await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(id.Value);

            TempData["Sucesso"] = "Classificação recalculada com sucesso.";

            return RedirectToAction(nameof(Classificacao), new { id });
        }

        public async Task<IActionResult> Portal(int? id)
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

            var proximosJogos = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.CampeonatoId == id &&
                    j.Ativo &&
                    j.DataJogo >= DateTime.Now &&
                    j.Status != "Finalizado")
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            var jogosFinalizados = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.CampeonatoId == id &&
                    j.Ativo &&
                    j.Status == "Finalizado")
                .OrderByDescending(j => j.DataJogo)
                .ToListAsync();

            var viewModel = new PortalCampeonatoViewModel
            {
                Campeonato = campeonato,
                Classificacoes = classificacoes,
                ProximosJogos = proximosJogos,
                JogosFinalizados = jogosFinalizados
            };

            return View(viewModel);
        }
    }
}
