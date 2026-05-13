using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
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

        public async Task<IActionResult> Ranking(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var liga = await _context.Ligas
                .Include(l => l.Campeonato)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (liga == null)
            {
                return NotFound();
            }

            var participantes = await _context.LigaParticipantes
                .Where(p => p.LigaId == id && p.Ativo)
                .OrderByDescending(p => p.PontuacaoTotal)
                .ThenBy(p => p.Nome)
                .ToListAsync();

            var ranking = new List<RankingParticipanteViewModel>();

            int posicao = 1;

            foreach (var participante in participantes)
            {
                var palpitesParticipante = await _context.Palpites
                    .Include(p => p.Jogo)
                    .Where(p =>
                        p.LigaId == id &&
                        p.LigaParticipanteId == participante.Id &&
                        p.Ativo)
                    .ToListAsync();

                var totalPalpites = palpitesParticipante.Count;

                var placaresExatos = palpitesParticipante.Count(p =>
                    p.Jogo != null &&
                    p.Jogo.Status == "Finalizado" &&
                    p.Jogo.GolsCasa.HasValue &&
                    p.Jogo.GolsVisitante.HasValue &&
                    p.Jogo.GolsCasa.Value == p.GolsCasaPalpite &&
                    p.Jogo.GolsVisitante.Value == p.GolsVisitantePalpite
                );

                ranking.Add(new RankingParticipanteViewModel
                {
                    Posicao = posicao,
                    Nome = participante.Nome,
                    Email = participante.Email,
                    PontuacaoTotal = participante.PontuacaoTotal,
                    TotalPalpites = totalPalpites,
                    PlacaresExatos = placaresExatos
                });

                posicao++;
            }

            var viewModel = new RankingLigaViewModel
            {
                Liga = liga,
                Participantes = ranking
            };

            return View(viewModel);
        }

    }
}