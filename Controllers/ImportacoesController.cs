using FutPlay.Data;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FutPlay.Controllers
{
    [Authorize(Roles = AppRoles.Administrador)]
    [EnableRateLimiting("CriticalActions")]
    public class ImportacoesController : Controller
    {
        private readonly ImportacaoCampeonatoService _importacaoCampeonatoService;
        private readonly CampeonatoSincronizacaoService _campeonatoSincronizacaoService;
        private readonly FootballDataOrgService _footballDataOrgService;
        private readonly MockDataService _mockDataService;
        private readonly AppDbContext _context;

        public ImportacoesController(
            ImportacaoCampeonatoService importacaoCampeonatoService,
            CampeonatoSincronizacaoService campeonatoSincronizacaoService,
            FootballDataOrgService footballDataOrgService,
            MockDataService mockDataService,
            AppDbContext context)
        {
            _importacaoCampeonatoService = importacaoCampeonatoService;
            _campeonatoSincronizacaoService = campeonatoSincronizacaoService;
            _footballDataOrgService = footballDataOrgService;
            _mockDataService = mockDataService;
            _context = context;

        }

        public async Task<IActionResult> Index()
        {
            await CarregarCampeonatosAsync();
            return View(new List<ApiLigaViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuscarLigas(string pais, int temporada)
        {
            var ligas = new List<ApiLigaViewModel>();

            try
            {
                ligas = await _importacaoCampeonatoService.BuscarLigasAsync(pais, temporada);

                ViewBag.Pais = pais;
                ViewBag.Temporada = temporada;
                ViewBag.TotalEncontrado = ligas.Count;
            }
            catch (Exception ex)
            {
                ViewBag.Erro = ex.Message;
            }

            await CarregarCampeonatosAsync();
            return View("Index", ligas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarLiga(
            int apiLeagueId,
            string nome,
            string tipo,
            string pais,
            string? logoUrl,
            int temporada)
        {
            var resultado = await _importacaoCampeonatoService.ImportarLigaAsync(
                apiLeagueId,
                nome,
                tipo,
                pais,
                logoUrl,
                temporada,
                ObterUsuarioId(),
                ObterUsuarioEmail()
            );

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarLiga(
            int apiLeagueId,
            string nome,
            string tipo,
            string pais,
            string? logoUrl,
            int temporada)
        {
            var resultado = await _importacaoCampeonatoService.AtualizarLigaAsync(
                apiLeagueId,
                nome,
                tipo,
                pais,
                logoUrl,
                temporada,
                usuarioId: ObterUsuarioId(),
                usuarioEmail: ObterUsuarioEmail()
            );

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarJogos(int campeonatoId)
        {
            var resultado = await _campeonatoSincronizacaoService.SincronizarJogosCompeticaoAsync(
                campeonatoId,
                ObterUsuarioId(),
                ObterUsuarioEmail());

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarResultados(int campeonatoId)
        {
            var resultado = await _campeonatoSincronizacaoService.AtualizarResultadosAsync(
                campeonatoId,
                ObterUsuarioId(),
                ObterUsuarioEmail());

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            if (resultado.RedirecionarParaPortal && resultado.CampeonatoId.HasValue)
            {
                return RedirectToAction("Portal", "Campeonatos", new { id = resultado.CampeonatoId.Value, aba = "jogos" });
            }

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SincronizarCampeonato(int campeonatoId)
        {
            var resultado = await _campeonatoSincronizacaoService.SincronizarCampeonatoAsync(
                campeonatoId,
                ObterUsuarioId(),
                ObterUsuarioEmail());

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            if (resultado.RedirecionarParaPortal && resultado.CampeonatoId.HasValue)
            {
                return RedirectToAction("Portal", "Campeonatos", new { id = resultado.CampeonatoId.Value, aba = "jogos" });
            }

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SincronizarJogos(int campeonatoId)
        {
            var resultado = await _campeonatoSincronizacaoService.SincronizarJogosCompeticaoAsync(
                campeonatoId,
                ObterUsuarioId(),
                ObterUsuarioEmail());

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            if (resultado.RedirecionarParaPortal && resultado.CampeonatoId.HasValue)
            {
                return RedirectToAction("Portal", "Campeonatos", new { id = resultado.CampeonatoId.Value, aba = "jogos" });
            }

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListarFootballDataOrgCompeticoes()
        {
            try
            {
                var competicoes = await _footballDataOrgService.ListarCompeticoesAsync();
                ViewBag.FootballDataOrgCompeticoes = competicoes;
                ViewBag.FootballDataOrgTotal = competicoes.Count;
            }
            catch (Exception ex)
            {
                TempData["Erro"] = ex.Message;
            }

            await CarregarCampeonatosAsync();
            return View("Index", new List<ApiLigaViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarResultadosFootballDataOrg(
            int campeonatoId,
            string competitionCode,
            int temporada)
        {
            var resultado = await _footballDataOrgService.AtualizarResultadosAsync(
                campeonatoId,
                competitionCode,
                temporada,
                ObterUsuarioId(),
                ObterUsuarioEmail());

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            return RedirectToAction("Portal", "Campeonatos", new { id = campeonatoId, aba = "jogos" });
        }

        [HttpGet]
        public async Task<IActionResult> Historico(string? tipo, string? status, int? campeonatoId)
        {
            var logs = _context.ApiSyncLogs
                .Include(l => l.Campeonato)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                logs = logs.Where(l => l.TipoSincronizacao == tipo);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                logs = logs.Where(l => l.Status == status);
            }

            if (campeonatoId.HasValue)
            {
                logs = logs.Where(l => l.CampeonatoId == campeonatoId.Value);
            }

            ViewBag.Tipo = tipo;
            ViewBag.Status = status;
            ViewBag.CampeonatoId = campeonatoId;
            ViewBag.Tipos = await _context.ApiSyncLogs
                .AsNoTracking()
                .Select(l => l.TipoSincronizacao)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
            ViewBag.Statuses = await _context.ApiSyncLogs
                .AsNoTracking()
                .Select(l => l.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
            ViewBag.Campeonatos = await _context.Campeonatos
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .ThenByDescending(c => c.Ano)
                .ToListAsync();

            var model = await logs
                .OrderByDescending(l => l.DataInicio)
                .ThenByDescending(l => l.Id)
                .Take(200)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarDadosTeste()
        {
            try
            {
                var mensagem = await _mockDataService.GerarDadosTesteAsync();

                TempData["Sucesso"] = mensagem;
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao gerar dados de teste: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private string? ObterUsuarioId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private string? ObterUsuarioEmail()
        {
            return User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        }

        private async Task CarregarCampeonatosAsync()
        {
            ViewBag.Campeonatos = await _context.Campeonatos
                .AsNoTracking()
                .Where(c => c.Ativo)
                .OrderByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .ToListAsync();
        }

    }
}
