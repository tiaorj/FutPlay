using FutPlay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FutPlay.Controllers
{
    [Authorize(Roles = AppRoles.Administrador)]
    [EnableRateLimiting("CriticalActions")]
    public class MockDataController : Controller
    {
        private readonly MockDataService _mockDataService;

        public MockDataController(MockDataService mockDataService)
        {
            _mockDataService = mockDataService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.DadosTesteExistem = await _mockDataService.DadosTesteExistemAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Gerar()
        {
            try
            {
                TempData["Sucesso"] = await _mockDataService.GerarDadosTesteAsync();
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao gerar dados de teste: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Limpar()
        {
            try
            {
                TempData["Sucesso"] = await _mockDataService.LimparDadosTesteAsync();
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao limpar dados de teste: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
