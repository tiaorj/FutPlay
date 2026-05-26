using FutPlay.Data;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class EstatisticasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ComparadorTimesService _comparadorTimesService;

        public EstatisticasController(
            AppDbContext context,
            ComparadorTimesService comparadorTimesService)
        {
            _context = context;
            _comparadorTimesService = comparadorTimesService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CompararTimes(
            int? timeAId,
            int? timeBId,
            int? campeonatoId)
        {
            var viewModel = new ComparadorTimesViewModel
            {
                TimeAId = timeAId,
                TimeBId = timeBId,
                CampeonatoId = campeonatoId,
                Times = await _context.Times
                    .AsNoTracking()
                    .Where(t => t.Ativo)
                    .OrderBy(t => t.Nome)
                    .ToListAsync(),
                Campeonatos = await _context.Campeonatos
                    .AsNoTracking()
                    .Where(c => c.Ativo)
                    .OrderByDescending(c => c.Ano)
                    .ThenBy(c => c.Nome)
                    .ToListAsync()
            };

            if (timeAId.HasValue && timeBId.HasValue)
            {
                if (timeAId.Value == timeBId.Value)
                {
                    viewModel.Mensagem = "Selecione dois times diferentes para comparar.";
                }
                else
                {
                    viewModel.Resultado = await _comparadorTimesService.CompararAsync(
                        timeAId.Value,
                        timeBId.Value,
                        campeonatoId);

                    if (viewModel.Resultado == null)
                    {
                        viewModel.Mensagem = "Não foi possível encontrar os times selecionados.";
                    }
                }
            }

            return View(viewModel);
        }
    }
}
