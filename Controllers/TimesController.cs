using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutPlay.ViewModels;
using System.Security.Claims;

namespace FutPlay.Controllers
{
    public class TimesController : Controller
    {
        private readonly AppDbContext _context;

        public TimesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filtro = "todos", string? pais = null)
        {
            filtro = NormalizarFiltro(filtro);

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

            var todosTimes = await _context.Times
                .OrderByDescending(t => t.Ativo)
                .ThenBy(t => t.Tipo)
                .ThenBy(t => t.Nome)
                .ToListAsync();

            IEnumerable<Time> times = todosTimes;

            times = filtro switch
            {
                "clubes" => times.Where(t => string.Equals(t.Tipo, "Clube", StringComparison.OrdinalIgnoreCase)),
                "selecoes" => times.Where(EhSelecao),
                "ativos" => times.Where(t => t.Ativo),
                "favoritos" => usuarioAutenticado
                    ? times.Where(t => timesFavoritosIds.Contains(t.Id))
                    : Enumerable.Empty<Time>(),
                _ => times
            };

            if (!string.IsNullOrWhiteSpace(pais))
            {
                times = times.Where(t => string.Equals(t.Pais, pais, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new TimesIndexViewModel
            {
                Times = times
                    .OrderByDescending(t => t.Ativo)
                    .ThenBy(t => t.Tipo)
                    .ThenBy(t => t.Nome)
                    .ToList(),
                Paises = todosTimes
                    .Select(t => t.Pais)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p)
                    .ToList(),
                TimesFavoritosIds = timesFavoritosIds,
                Filtro = filtro,
                Pais = pais,
                UsuarioAutenticado = usuarioAutenticado,
                TotalTimes = todosTimes.Count,
                TotalAtivos = todosTimes.Count(t => t.Ativo),
                TotalClubes = todosTimes.Count(t => string.Equals(t.Tipo, "Clube", StringComparison.OrdinalIgnoreCase)),
                TotalSelecoes = todosTimes.Count(EhSelecao),
                TotalFavoritos = todosTimes.Count(t => timesFavoritosIds.Contains(t.Id))
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlternarFavorito(int id, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["Erro"] = "Entre na sua conta para favoritar times.";
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity", returnUrl = ObterReturnUrlSeguro(returnUrl) });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Erro"] = "Não foi possível identificar seu usuário.";
                return Redirect(ObterReturnUrlSeguro(returnUrl));
            }

            var timeExiste = await _context.Times.AnyAsync(t => t.Id == id);

            if (!timeExiste)
            {
                return NotFound();
            }

            var favorito = await _context.TimeFavoritos
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TimeId == id);

            if (favorito == null)
            {
                _context.TimeFavoritos.Add(new TimeFavorito
                {
                    UserId = userId,
                    TimeId = id
                });

                TempData["Sucesso"] = "Time adicionado aos favoritos.";
            }
            else
            {
                _context.TimeFavoritos.Remove(favorito);
                TempData["Sucesso"] = "Time removido dos favoritos.";
            }

            await _context.SaveChangesAsync();

            return Redirect(ObterReturnUrlSeguro(returnUrl));
        }

        [Authorize(Roles = AppRoles.Administrador)]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = AppRoles.Administrador)]
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

            var jogosDoTime = await _context.Jogos
                .Include(j => j.Campeonato)
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.Ativo &&
                    (j.TimeCasaId == time.Id || j.TimeVisitanteId == time.Id))
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            var proximosJogos = jogosDoTime
                .Where(j =>
                    j.DataJogo >= DateTime.Now &&
                    !string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                .OrderBy(j => j.DataJogo)
                .Take(6)
                .ToList();

            var ultimosResultados = jogosDoTime
                .Where(j =>
                    string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(j => j.DataJogo)
                .Take(6)
                .ToList();

            var campeonatos = jogosDoTime
                .Where(j => j.Campeonato != null)
                .Select(j => j.Campeonato!)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .ToList();

            int totalVitorias = 0;
            int totalEmpates = 0;
            int totalDerrotas = 0;
            int golsPro = 0;
            int golsContra = 0;

            foreach (var jogo in jogosDoTime.Where(j =>
                string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                j.GolsCasa.HasValue &&
                j.GolsVisitante.HasValue))
            {
                bool timeCasa = jogo.TimeCasaId == time.Id;

                int golsTime = timeCasa ? jogo.GolsCasa!.Value : jogo.GolsVisitante!.Value;
                int golsAdversario = timeCasa ? jogo.GolsVisitante!.Value : jogo.GolsCasa!.Value;

                golsPro += golsTime;
                golsContra += golsAdversario;

                if (golsTime > golsAdversario)
                {
                    totalVitorias++;
                }
                else if (golsTime < golsAdversario)
                {
                    totalDerrotas++;
                }
                else
                {
                    totalEmpates++;
                }
            }

            var viewModel = new TimeDetalhesViewModel
            {
                Time = time,
                ProximosJogos = proximosJogos,
                UltimosResultados = ultimosResultados,
                Campeonatos = campeonatos,
                TotalJogos = jogosDoTime.Count,
                TotalVitorias = totalVitorias,
                TotalEmpates = totalEmpates,
                TotalDerrotas = totalDerrotas,
                GolsPro = golsPro,
                GolsContra = golsContra
            };

            return View(viewModel);
        }

        [Authorize(Roles = AppRoles.Administrador)]
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

        [Authorize(Roles = AppRoles.Administrador)]
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

        private string ObterReturnUrlSeguro(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action(nameof(Index), "Times") ?? "/";
        }

        private static string NormalizarFiltro(string? filtro)
        {
            return filtro?.ToLowerInvariant() switch
            {
                "clubes" => "clubes",
                "selecoes" => "selecoes",
                "ativos" => "ativos",
                "favoritos" => "favoritos",
                _ => "todos"
            };
        }

        private static bool EhSelecao(Time time)
        {
            return string.Equals(time.Tipo, "Seleção", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(time.Tipo, "Selecao", StringComparison.OrdinalIgnoreCase);
        }
    }
}
