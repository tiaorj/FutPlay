using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FutPlay.Controllers
{
    public class JogosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;

        public JogosController(
            AppDbContext context,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService)
        {
            _context = context;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
        }

        public async Task<IActionResult> Index(
            string aba = "todos",
            string filtro = "todos",
            int? rodada = null,
            int? campeonatoId = null)
        {
            aba = NormalizarAba(aba);
            filtro = NormalizarFiltro(filtro);

            var hoje = DateTime.Today;
            var usuarioAutenticado = User.Identity?.IsAuthenticated == true;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var timesFavoritosIds = new HashSet<int>();

            if (usuarioAutenticado && !string.IsNullOrWhiteSpace(userId))
            {
                timesFavoritosIds = (await _context.TimeFavoritos
                    .Where(f => f.UserId == userId)
                    .Select(f => f.TimeId)
                    .ToListAsync())
                    .ToHashSet();
            }

            var jogosBase = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j => j.Ativo)
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            IEnumerable<Jogo> jogosDaAba = jogosBase;

            if (aba == "favoritos")
            {
                jogosDaAba = usuarioAutenticado
                    ? jogosDaAba.Where(j =>
                        timesFavoritosIds.Contains(j.TimeCasaId) ||
                        timesFavoritosIds.Contains(j.TimeVisitanteId))
                    : Enumerable.Empty<Jogo>();
            }

            var campeonatos = jogosDaAba
                .Where(j => j.Campeonato != null)
                .GroupBy(j => j.CampeonatoId)
                .Select(g => new CampeonatoFiltroViewModel
                {
                    Id = g.Key,
                    Nome = g.First().Campeonato?.Nome ?? "Campeonato",
                    Pais = g.First().Campeonato?.Pais,
                    Tipo = g.First().Campeonato?.Tipo,
                    LogoUrl = g.First().Campeonato?.LogoUrl,
                    Ano = g.First().Campeonato?.Ano ?? 0,
                    TotalJogos = g.Count(),
                    Selecionado = campeonatoId == g.Key
                })
                .OrderBy(c => c.Nome)
                .ToList();

            if (campeonatoId.HasValue)
            {
                jogosDaAba = jogosDaAba.Where(j => j.CampeonatoId == campeonatoId.Value);
            }

            var jogosContexto = jogosDaAba.ToList();
            var navegarPorRodada = campeonatoId.HasValue;

            var rodadas = navegarPorRodada
                ? jogosContexto
                    .Where(j => j.Rodada.HasValue)
                    .GroupBy(j => j.Rodada!.Value)
                    .Select(g => new RodadaFiltroViewModel
                    {
                        Rodada = g.Key,
                        DataReferencia = g.Min(j => j.DataJogo.Date),
                        TotalJogos = g.Count()
                    })
                    .OrderBy(r => r.Rodada)
                    .ToList()
                : new List<RodadaFiltroViewModel>();

            var rodadaSelecionada = navegarPorRodada ? rodada : null;

            if (rodadaSelecionada.HasValue && !rodadas.Any(r => r.Rodada == rodadaSelecionada.Value))
            {
                rodadaSelecionada = null;
            }

            if (!rodadaSelecionada.HasValue && rodadas.Any())
            {
                rodadaSelecionada = rodadas
                    .OrderBy(r => Math.Abs((r.DataReferencia - hoje).TotalDays))
                    .ThenBy(r => r.Rodada)
                    .First()
                    .Rodada;
            }

            foreach (var rodadaOpcao in rodadas)
            {
                rodadaOpcao.Selecionada = rodadaOpcao.Rodada == rodadaSelecionada;
            }

            var rodadasOrdenadas = rodadas.Select(r => r.Rodada).ToList();
            int? rodadaAnterior = null;
            int? proximaRodada = null;

            if (rodadaSelecionada.HasValue)
            {
                var rodadaIndex = rodadasOrdenadas.IndexOf(rodadaSelecionada.Value);

                if (rodadaIndex > 0)
                {
                    rodadaAnterior = rodadasOrdenadas[rodadaIndex - 1];
                }

                if (rodadaIndex >= 0 && rodadaIndex < rodadasOrdenadas.Count - 1)
                {
                    proximaRodada = rodadasOrdenadas[rodadaIndex + 1];
                }
            }

            var jogosDaRodada = navegarPorRodada &&
                rodadaSelecionada.HasValue &&
                rodadas.Any(r => r.Rodada == rodadaSelecionada.Value)
                ? jogosContexto.Where(j => j.Rodada == rodadaSelecionada.Value).ToList()
                : jogosContexto;

            var jogosFiltrados = AplicarFiltro(jogosDaRodada, filtro, hoje)
                .OrderBy(j => j.DataJogo)
                .ToList();

            var viewModel = new JogosIndexViewModel
            {
                Jogos = jogosFiltrados,
                Rodadas = rodadas,
                Campeonatos = campeonatos,
                TimesFavoritosIds = timesFavoritosIds,
                Aba = aba,
                Filtro = filtro,
                RodadaSelecionada = rodadaSelecionada,
                RodadaAnterior = rodadaAnterior,
                ProximaRodada = proximaRodada,
                CampeonatoId = campeonatoId,
                UsuarioAutenticado = usuarioAutenticado,
                TotalJogos = jogosDaRodada.Count,
                TotalHoje = jogosDaRodada.Count(j => j.DataJogo.Date == hoje),
                TotalProximos = jogosDaRodada.Count(j => EhProximo(j, hoje)),
                TotalFinalizados = jogosDaRodada.Count(EhFinalizado),
                TotalFavoritos = jogosBase.Count(j =>
                    timesFavoritosIds.Contains(j.TimeCasaId) ||
                    timesFavoritosIds.Contains(j.TimeVisitanteId)),
                TotalTimesFavoritos = timesFavoritosIds.Count
            };

            return View(viewModel);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        public async Task<IActionResult> Create()
        {
            await CarregarCombos();
            return View();
        }

        [Authorize(Roles = AppRoles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Jogo jogo)
        {
            if (jogo.TimeCasaId == jogo.TimeVisitanteId)
            {
                ModelState.AddModelError("", "O time da casa e o time visitante não podem ser iguais.");
            }

            if (ModelState.IsValid)
            {
                _context.Jogos.Add(jogo);
                await _context.SaveChangesAsync();

                await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(jogo.CampeonatoId);

                if (AfetaClassificacao(jogo))
                {
                    await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(jogo.CampeonatoId);
                }

                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(jogo);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var jogo = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jogo == null)
                return NotFound();

            return View(jogo);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var jogo = await _context.Jogos.FindAsync(id);

            if (jogo == null)
                return NotFound();

            await CarregarCombos();
            return View(jogo);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Jogo jogo)
        {
            if (id != jogo.Id)
                return NotFound();

            if (jogo.TimeCasaId == jogo.TimeVisitanteId)
            {
                ModelState.AddModelError("", "O time da casa e o time visitante não podem ser iguais.");
            }

            var jogoAnterior = await _context.Jogos
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jogoAnterior == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(jogo);
                await _context.SaveChangesAsync();

                var campeonatosParaRecalcular = ObterCampeonatosParaRecalculo(jogoAnterior, jogo);

                foreach (var campeonatoId in campeonatosParaRecalcular)
                {
                    await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonatoId);
                }

                if (AfetaClassificacao(jogoAnterior) || AfetaClassificacao(jogo))
                {
                    foreach (var campeonatoId in campeonatosParaRecalcular)
                    {
                        await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonatoId);
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            await CarregarCombos();
            return View(jogo);
        }

        private async Task CarregarCombos()
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

            ViewBag.TimeCasaId = new SelectList(
                await _context.Times
                    .Where(t => t.Ativo)
                    .OrderBy(t => t.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );

            ViewBag.TimeVisitanteId = new SelectList(
                await _context.Times
                    .Where(t => t.Ativo)
                    .OrderBy(t => t.Nome)
                    .ToListAsync(),
                "Id",
                "Nome"
            );
        }

        private static IEnumerable<Jogo> AplicarFiltro(
            IEnumerable<Jogo> jogos,
            string filtro,
            DateTime hoje)
        {
            return filtro switch
            {
                "hoje" => jogos.Where(j => j.DataJogo.Date == hoje),
                "proximos" => jogos.Where(j => EhProximo(j, hoje)),
                "finalizados" => jogos.Where(EhFinalizado),
                _ => jogos
            };
        }

        private static bool EhFinalizado(Jogo jogo)
        {
            return string.Equals(jogo.Status, "Finalizado", StringComparison.OrdinalIgnoreCase);
        }

        private static bool AfetaClassificacao(Jogo jogo)
        {
            return jogo.Ativo &&
                   EhFinalizado(jogo) &&
                   jogo.GolsCasa.HasValue &&
                   jogo.GolsVisitante.HasValue;
        }

        private static HashSet<int> ObterCampeonatosParaRecalculo(Jogo jogoAnterior, Jogo jogoAtual)
        {
            return new HashSet<int>
            {
                jogoAnterior.CampeonatoId,
                jogoAtual.CampeonatoId
            };
        }

        private static bool EhProximo(Jogo jogo, DateTime hoje)
        {
            return jogo.DataJogo.Date >= hoje && !EhFinalizado(jogo);
        }

        private static string NormalizarAba(string? aba)
        {
            return aba?.ToLowerInvariant() switch
            {
                "favoritos" => "favoritos",
                "competicoes" => "competicoes",
                _ => "todos"
            };
        }

        private static string NormalizarFiltro(string? filtro)
        {
            return filtro?.ToLowerInvariant() switch
            {
                "hoje" => "hoje",
                "proximos" => "proximos",
                "finalizados" => "finalizados",
                _ => "todos"
            };
        }
    }
}
