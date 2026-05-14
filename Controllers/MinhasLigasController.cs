using FutPlay.Data;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    [Authorize]
    public class MinhasLigasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MinhasLigasController(
            AppDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
            {
                return Challenge();
            }

            await VincularParticipantesAntigosAsync(usuario);

            var emailUsuarioNormalizado = (usuario.Email ?? string.Empty).ToUpper();

            var minhasLigas = await _context.LigaParticipantes
                .Include(p => p.Liga)
                    .ThenInclude(l => l!.Campeonato)
                .Where(p =>
                    p.Ativo &&
                    (p.UserId == usuario.Id ||
                     (p.UserId == null && p.Email.ToUpper() == emailUsuarioNormalizado)))
                .OrderBy(p => p.Liga!.Nome)
                .Select(p => new MinhaLigaViewModel
                {
                    LigaId = p.LigaId,
                    LigaParticipanteId = p.Id,
                    NomeLiga = p.Liga!.Nome,
                    NomeCampeonato = p.Liga.Campeonato != null ? p.Liga.Campeonato.Nome : "",
                    Pontuacao = p.PontuacaoTotal
                })
                .ToListAsync();

            return View(minhasLigas);
        }

        private async Task VincularParticipantesAntigosAsync(IdentityUser usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                return;
            }

            var emailUsuarioNormalizado = usuario.Email.ToUpper();

            var participantes = await _context.LigaParticipantes
                .Where(p =>
                    p.Ativo &&
                    p.UserId == null &&
                    p.Email.ToUpper() == emailUsuarioNormalizado)
                .ToListAsync();

            if (!participantes.Any())
            {
                return;
            }

            foreach (var participante in participantes)
            {
                participante.UserId = usuario.Id;
            }

            await _context.SaveChangesAsync();
        }
    }
}
