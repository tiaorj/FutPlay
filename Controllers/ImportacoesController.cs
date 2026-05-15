using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FutPlay.Controllers
{
    [Authorize(Roles = AppRoles.Administrador)]
    [EnableRateLimiting("CriticalActions")]
    public class ImportacoesController : Controller
    {
        private readonly ImportacaoCampeonatoService _importacaoCampeonatoService;
        private readonly ImportacaoJogosService _importacaoJogosService;
        private readonly CampeonatoSincronizacaoService _campeonatoSincronizacaoService;
        private readonly MockDataService _mockDataService;

        public ImportacoesController(
            ImportacaoCampeonatoService importacaoCampeonatoService,
            ImportacaoJogosService importacaoJogosService,
            CampeonatoSincronizacaoService campeonatoSincronizacaoService,
            MockDataService mockDataService)
        {
            _importacaoCampeonatoService = importacaoCampeonatoService;
            _importacaoJogosService = importacaoJogosService;
            _campeonatoSincronizacaoService = campeonatoSincronizacaoService;
            _mockDataService = mockDataService;

        }

        public IActionResult Index()
        {
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
            bool importado = await _importacaoCampeonatoService.ImportarLigaAsync(
                apiLeagueId,
                nome,
                tipo,
                pais,
                logoUrl,
                temporada
            );

            if (!importado)
            {
                TempData["Erro"] = "Este campeonato já foi importado.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Sucesso"] = $"Campeonato {nome} importado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarJogos(int campeonatoId)
        {
            var resultado = await _importacaoJogosService.ImportarJogosAsync(campeonatoId);

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarResultados(int campeonatoId)
        {
            var resultado = await _campeonatoSincronizacaoService.AtualizarResultadosAsync(campeonatoId);

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SincronizarCampeonato(int campeonatoId)
        {
            var resultado = await _campeonatoSincronizacaoService.SincronizarCampeonatoAsync(campeonatoId);

            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;

            if (resultado.RedirecionarParaPortal && resultado.CampeonatoId.HasValue)
            {
                return RedirectToAction("Portal", "Campeonatos", new { id = resultado.CampeonatoId.Value });
            }

            return RedirectToAction("Index", "Campeonatos");
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

    }
}
