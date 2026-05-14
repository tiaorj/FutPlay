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
            // Validações adicionais no backend
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

            // Validações adicionais no backend (mesmas regras do Create)
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

            var jogosFinalizados = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.CampeonatoId == id &&
                    j.Ativo &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue)
                .ToListAsync();

            var classificacoesAtuais = await _context.Classificacoes
                .Where(c => c.CampeonatoId == id)
                .ToListAsync();

            _context.Classificacoes.RemoveRange(classificacoesAtuais);

            var tabela = new Dictionary<int, Classificacao>();

            foreach (var jogo in jogosFinalizados)
            {
                if (!tabela.ContainsKey(jogo.TimeCasaId))
                {
                    tabela[jogo.TimeCasaId] = CriarClassificacaoInicial(id.Value, jogo.TimeCasaId, jogo.Grupo);
                }

                if (!tabela.ContainsKey(jogo.TimeVisitanteId))
                {
                    tabela[jogo.TimeVisitanteId] = CriarClassificacaoInicial(id.Value, jogo.TimeVisitanteId, jogo.Grupo);
                }

                var casa = tabela[jogo.TimeCasaId];
                var visitante = tabela[jogo.TimeVisitanteId];

                int golsCasa = jogo.GolsCasa.Value;
                int golsVisitante = jogo.GolsVisitante.Value;

                casa.Jogos++;
                visitante.Jogos++;

                casa.GolsPro += golsCasa;
                casa.GolsContra += golsVisitante;

                visitante.GolsPro += golsVisitante;
                visitante.GolsContra += golsCasa;

                casa.SaldoGols = casa.GolsPro - casa.GolsContra;
                visitante.SaldoGols = visitante.GolsPro - visitante.GolsContra;

                if (golsCasa > golsVisitante)
                {
                    casa.Vitorias++;
                    casa.Pontos += 3;

                    visitante.Derrotas++;
                }
                else if (golsVisitante > golsCasa)
                {
                    visitante.Vitorias++;
                    visitante.Pontos += 3;

                    casa.Derrotas++;
                }
                else
                {
                    casa.Empates++;
                    visitante.Empates++;

                    casa.Pontos += 1;
                    visitante.Pontos += 1;
                }
            }

            var classificacoesOrdenadas = tabela.Values
                .OrderBy(c => c.Grupo)
                .ThenByDescending(c => c.Pontos)
                .ThenByDescending(c => c.Vitorias)
                .ThenByDescending(c => c.SaldoGols)
                .ThenByDescending(c => c.GolsPro)
                .ToList();

            var grupos = classificacoesOrdenadas
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Grupo) ? "" : c.Grupo);

            foreach (var grupo in grupos)
            {
                int posicao = 1;

                foreach (var item in grupo)
                {
                    item.Posicao = posicao;
                    posicao++;
                }
            }

            _context.Classificacoes.AddRange(classificacoesOrdenadas);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Classificação recalculada com sucesso.";

            return RedirectToAction(nameof(Classificacao), new { id });
        }

        private Classificacao CriarClassificacaoInicial(int campeonatoId, int timeId, string? grupo)
        {
            return new Classificacao
            {
                CampeonatoId = campeonatoId,
                TimeId = timeId,
                Grupo = grupo,
                Posicao = 0,
                Pontos = 0,
                Jogos = 0,
                Vitorias = 0,
                Empates = 0,
                Derrotas = 0,
                GolsPro = 0,
                GolsContra = 0,
                SaldoGols = 0,
                Ativo = true
            };
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