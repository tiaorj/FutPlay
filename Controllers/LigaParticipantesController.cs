using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
    public class LigaParticipantesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LigaParticipantesController(
            AppDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var participantes = await _context.LigaParticipantes
                .Include(p => p.Liga)
                .OrderBy(p => p.Liga!.Nome)
                .ThenBy(p => p.Nome)
                .ToListAsync();

            return View(participantes);
        }

        public async Task<IActionResult> Create()
        {
            await CarregarLigas();

            return View(new LigaParticipante
            {
                DataEntrada = DateTime.Now,
                Ativo = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LigaParticipante participante)
        {
            participante.DataEntrada = DateTime.Now;

            var emailJaExisteNaLiga = await _context.LigaParticipantes
                .AnyAsync(p => p.LigaId == participante.LigaId && p.Email == participante.Email);

            if (emailJaExisteNaLiga)
            {
                ModelState.AddModelError("Email", "Este e-mail já está cadastrado nesta liga.");
            }

            if (ModelState.IsValid)
            {
                await VincularUsuarioLogadoPorEmailAsync(participante);

                _context.LigaParticipantes.Add(participante);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarLigas();
            return View(participante);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var participante = await _context.LigaParticipantes
                .Include(p => p.Liga)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participante == null)
                return NotFound();

            return View(participante);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var participante = await _context.LigaParticipantes.FindAsync(id);

            if (participante == null)
                return NotFound();

            await CarregarLigas();
            return View(participante);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LigaParticipante participante)
        {
            if (id != participante.Id)
                return NotFound();

            var emailJaExisteNaLiga = await _context.LigaParticipantes
                .AnyAsync(p =>
                    p.Id != participante.Id &&
                    p.LigaId == participante.LigaId &&
                    p.Email == participante.Email);

            if (emailJaExisteNaLiga)
            {
                ModelState.AddModelError("Email", "Este e-mail já está cadastrado nesta liga.");
            }

            if (ModelState.IsValid)
            {
                var participanteAtual = await _context.LigaParticipantes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                await VincularUsuarioLogadoPorEmailAsync(participante);

                if (participante.UserId == null &&
                    participanteAtual?.UserId != null &&
                    string.Equals(participante.Email, participanteAtual.Email, StringComparison.OrdinalIgnoreCase))
                {
                    participante.UserId = participanteAtual.UserId;
                }

                _context.Update(participante);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CarregarLigas();
            return View(participante);
        }

        private async Task CarregarLigas()
        {
            ViewBag.LigaId = new SelectList(
                await _context.Ligas
                    .Where(l => l.Ativo)
                    .OrderBy(l => l.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );
        }

        private async Task VincularUsuarioLogadoPorEmailAsync(LigaParticipante participante)
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario?.Email == null)
            {
                return;
            }

            if (string.Equals(participante.Email, usuario.Email, StringComparison.OrdinalIgnoreCase))
            {
                participante.UserId = usuario.Id;
            }
        }
    }
}
