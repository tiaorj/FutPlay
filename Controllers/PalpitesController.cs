using FutPlay.Data;
using FutPlay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class PalpitesController : Controller
    {
        private readonly AppDbContext _context;

        public PalpitesController(AppDbContext context)
        {
            _context = context;
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
                return NotFound();

            var palpite = await _context.Palpites
                .Include(p => p.Liga)
                .Include(p => p.LigaParticipante)
                .Include(p => p.Jogo)
                    .ThenInclude(j => j!.TimeCasa)
                .Include(p => p.Jogo)
                    .ThenInclude(j => j!.TimeVisitante)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (palpite == null)
                return NotFound();

            return View(palpite);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var palpite = await _context.Palpites.FindAsync(id);

            if (palpite == null)
                return NotFound();

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
                return NotFound();

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

        private int CalcularPontuacao(Palpite palpite, Jogo jogo)
        {
            if (!jogo.GolsCasa.HasValue || !jogo.GolsVisitante.HasValue)
            {
                return 0;
            }

            int golsCasaReal = jogo.GolsCasa.Value;
            int golsVisitanteReal = jogo.GolsVisitante.Value;

            int golsCasaPalpite = palpite.GolsCasaPalpite;
            int golsVisitantePalpite = palpite.GolsVisitantePalpite;

            // Placar exato
            if (golsCasaReal == golsCasaPalpite &&
                golsVisitanteReal == golsVisitantePalpite)
            {
                return 10;
            }

            int pontos = 0;

            // Acertou vencedor ou empate
            string resultadoReal = ObterResultado(golsCasaReal, golsVisitanteReal);
            string resultadoPalpite = ObterResultado(golsCasaPalpite, golsVisitantePalpite);

            if (resultadoReal == resultadoPalpite)
            {
                pontos += 5;
            }

            // Acertou gols do time da casa
            if (golsCasaReal == golsCasaPalpite)
            {
                pontos += 2;
            }

            // Acertou gols do time visitante
            if (golsVisitanteReal == golsVisitantePalpite)
            {
                pontos += 2;
            }

            return pontos;
        }

        private string ObterResultado(int golsCasa, int golsVisitante)
        {
            if (golsCasa > golsVisitante)
            {
                return "Casa";
            }

            if (golsVisitante > golsCasa)
            {
                return "Visitante";
            }

            return "Empate";
        }

        public async Task<IActionResult> RecalcularPontuacao()
        {
            var palpites = await _context.Palpites
                .Include(p => p.Jogo)
                .Where(p => p.Ativo)
                .ToListAsync();

            foreach (var palpite in palpites)
            {
                if (palpite.Jogo != null &&
                    palpite.Jogo.Status == "Finalizado" &&
                    palpite.Jogo.GolsCasa.HasValue &&
                    palpite.Jogo.GolsVisitante.HasValue)
                {
                    palpite.PontosGanhos = CalcularPontuacao(palpite, palpite.Jogo);
                }
                else
                {
                    palpite.PontosGanhos = 0;
                }
            }

            await _context.SaveChangesAsync();

            await AtualizarPontuacaoParticipantes();

            TempData["Sucesso"] = "Pontuação recalculada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        private async Task AtualizarPontuacaoParticipantes()
        {
            var participantes = await _context.LigaParticipantes.ToListAsync();

            foreach (var participante in participantes)
            {
                participante.PontuacaoTotal = await _context.Palpites
                    .Where(p =>
                        p.LigaParticipanteId == participante.Id &&
                        p.LigaId == participante.LigaId &&
                        p.Ativo)
                    .SumAsync(p => p.PontosGanhos);
            }

            await _context.SaveChangesAsync();
        }

    }
}