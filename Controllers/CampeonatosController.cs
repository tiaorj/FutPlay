using FutPlay.Models;
using Microsoft.AspNetCore.Mvc;

namespace FutPlay.Controllers
{
    public class CampeonatosController : Controller
    {
        public IActionResult Index()
        {
            var campeonatos = new List<Campeonato>
            {
                new Campeonato
                {
                    Id = 1,
                    Nome = "Copa do Mundo",
                    Ano = 2026,
                    Tipo = "Mundial",
                    DataInicio = new DateTime(2026, 6, 11),
                    DataFim = new DateTime(2026, 7, 19),
                    Ativo = true
                },
                new Campeonato
                {
                    Id = 2,
                    Nome = "Brasileirão Série A",
                    Ano = 2026,
                    Tipo = "Nacional",
                    Ativo = true
                }
            };

            return View(campeonatos);
        }
    }
}