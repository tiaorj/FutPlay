using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
    public class PalpitesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PontuacaoService _pontuacaoService;

        public PalpitesController(
            AppDbContext context,
            PontuacaoService pontuacaoService)
        {
            _context = context;
            _pontuacaoService = pontuacaoService;
        }

        public async Task<IActionResult> Index()
        {
            var palpites = await _context.Palpites
                .Include(p => p.Liga)
                .Include(p => p.LigaParticipante)
                .Include(p => p.Jogo)
                    .ThenInclude(j => j!.TimeCasa)
                .Include(p => p.Jogo)
                    .ThenInclude(j => j!.TimeVisitante)
                .OrderByDescending(p => p.DataPalpite)
                .ToListAsync();

            return View(palpites);
        }

        public async Task<IActionResult> Create()
        {
            await CarregarCombos();

            return View(new Palpite
            {
                DataPalpite = DateTime.Now,
                Ativo = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Palpite palpite)
        {
            palpite.DataPalpite = DateTime.Now;

            var jaExiste = await _context.Palpites
                .AnyAsync(p =>
                    p.LigaId == palpite.LigaId &&
                    p.LigaParticipanteId == palpite.LigaParticipanteId &&
                    p.JogoId == palpite.JogoId);

            if (jaExiste)
            {
                ModelState.AddModelError("", "Este participante já fez palpite para este jogo nesta liga.");
            }

            var jogo = await _context.Jogos.FindAsync(palpite.JogoId);

            if (jogo != null && jogo.DataJogo <= DateTime.Now)
            {
                ModelState.AddModelError("", "Não é possível cadastrar palpite após o início do jogo.");
            }

            if (ModelState.IsValid)
            {
                _context.Palpites.Add(palpite);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(palpite);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var palpite = await _context.Palpites
                .Include(p => p.Liga)
                .Include(p => p.LigaParticipante)
                .Include(p => p.Jogo)
                    .ThenInclude(j => j!.TimeCasa)
                .Include(p => p.Jogo)
                    .ThenInclude(j => j!.TimeVisitante)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (palpite == null)
            {
                return NotFound();
            }

            return View(palpite);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var palpite = await _context.Palpites.FindAsync(id);

            if (palpite == null)
            {
                return NotFound();
            }

            var jogo = await _context.Jogos.FindAsync(palpite.JogoId);

            if (jogo != null && jogo.DataJogo <= DateTime.Now)
            {
                TempData["Erro"] = "Não é possível editar palpite após o início do jogo.";
                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(palpite);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Palpite palpite)
        {
            if (id != palpite.Id)
            {
                return NotFound();
            }

            var jogo = await _context.Jogos.FindAsync(palpite.JogoId);

            if (jogo != null && jogo.DataJogo <= DateTime.Now)
            {
                ModelState.AddModelError("", "Não é possível editar palpite após o início do jogo.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(palpite);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(palpite);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        public async Task<IActionResult> RecalcularPontuacao()
        {
            await _pontuacaoService.RecalcularPontuacaoPalpitesAsync();

            TempData["Sucesso"] = "Pontuação recalculada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        private async Task CarregarCombos()
        {
            ViewBag.LigaId = new SelectList(
                await _context.Ligas
                    .Where(l => l.Ativo)
                    .OrderBy(l => l.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );

            ViewBag.LigaParticipanteId = new SelectList(
                await _context.LigaParticipantes
                    .Where(p => p.Ativo)
                    .OrderBy(p => p.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );

            var jogos = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j => j.Ativo)
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            ViewBag.JogoId = new SelectList(
                jogos.Select(j => new
                {
                    j.Id,
                    Descricao = $"{j.TimeCasa!.Nome} x {j.TimeVisitante!.Nome} - {j.DataJogo:dd/MM/yyyy HH:mm}"
                }),
                "Id",
                "Descricao"
            );
        }
    }
}
