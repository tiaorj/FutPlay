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
            var query = _context.LigaParticipantes
                .Include(p => p.Liga)
                .AsQueryable();

            query = AplicarFiltroUsuario(query);

            var participantes = await query
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

            if (!UsuarioEhAdministrador())
            {
                var usuario = await _userManager.GetUserAsync(User);

                if (usuario?.Email == null)
                {
                    return Challenge();
                }

                participante.Email = usuario.Email;
                participante.UserId = usuario.Id;
                participante.Ativo = true;
                participante.PontuacaoTotal = 0;
            }

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

            if (!UsuarioPodeAcessarParticipante(participante))
                return Forbid();

            return View(participante);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var participante = await _context.LigaParticipantes.FindAsync(id);

            if (participante == null)
                return NotFound();

            if (!UsuarioPodeAcessarParticipante(participante))
                return Forbid();

            await CarregarLigas();
            return View(participante);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LigaParticipante participante)
        {
            if (id != participante.Id)
                return NotFound();

            var participanteAtualParaPermissao = await _context.LigaParticipantes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participanteAtualParaPermissao == null)
                return NotFound();

            if (!UsuarioPodeAcessarParticipante(participanteAtualParaPermissao))
                return Forbid();

            if (!UsuarioEhAdministrador())
            {
                var usuario = await _userManager.GetUserAsync(User);

                if (usuario?.Email == null)
                {
                    return Challenge();
                }

                participante.LigaId = participanteAtualParaPermissao.LigaId;
                participante.Email = usuario.Email;
                participante.UserId = usuario.Id;
                participante.PontuacaoTotal = participanteAtualParaPermissao.PontuacaoTotal;
                participante.DataEntrada = participanteAtualParaPermissao.DataEntrada;
                participante.Ativo = participanteAtualParaPermissao.Ativo;
            }

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

        private bool UsuarioEhAdministrador()
        {
            return User.IsInRole(AppRoles.Administrador);
        }

        private IQueryable<LigaParticipante> AplicarFiltroUsuario(IQueryable<LigaParticipante> query)
        {
            if (UsuarioEhAdministrador())
            {
                return query;
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;
            var emailNormalizado = email?.ToUpper();

            return query.Where(p =>
                (userId != null && p.UserId == userId) ||
                (emailNormalizado != null && p.Email.ToUpper() == emailNormalizado));
        }

        private bool UsuarioPodeAcessarParticipante(LigaParticipante participante)
        {
            if (UsuarioEhAdministrador())
            {
                return true;
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            return (!string.IsNullOrWhiteSpace(userId) && participante.UserId == userId) ||
                (!string.IsNullOrWhiteSpace(email) &&
                 string.Equals(participante.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
