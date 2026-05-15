using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class LigasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LigasController> _logger;
        private const int MinutosBloqueioPalpite = 30;

        public LigasController(AppDbContext context, ILogger<LigasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        public async Task<IActionResult> Index()
        {
            var ligas = await _context.Ligas
                .Include(l => l.Campeonato)
                .OrderByDescending(l => l.Ativo)
                .ThenBy(l => l.Nome)
                .ToListAsync();

            return View(ligas);
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        public async Task<IActionResult> Palpitar(int? id, int? participanteId, string? origem)
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

            bool veioDeMinhasLigas = string.Equals(origem, "minhasligas", StringComparison.OrdinalIgnoreCase);

            if (veioDeMinhasLigas)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                var participanteUsuario = await _context.LigaParticipantes
                    .FirstOrDefaultAsync(p =>
                        p.LigaId == liga.Id &&
                        p.UserId == userId &&
                        p.Ativo);

                if (participanteUsuario == null)
                {
                    TempData["Erro"] = "Não encontramos um participante vinculado ao seu usuário nesta liga.";
                    return RedirectToAction("Index", "MinhasLigas");
                }

                var modelMinhasLigas = new PalpitarLigaViewModel
                {
                    LigaParticipanteId = participanteUsuario.Id,
                    ParticipanteBloqueado = true,
                    NomeParticipanteSelecionado = participanteUsuario.Nome,
                    Origem = "minhasligas"
                };

                var viewModelMinhasLigas = await MontarViewModelPalpitar(liga.Id, modelMinhasLigas);

                return View(viewModelMinhasLigas);
            }

            var model = new PalpitarLigaViewModel
            {
                LigaParticipanteId = participanteId ?? 0,
                ParticipanteBloqueado = false,
                Origem = origem
            };

            var viewModel = await MontarViewModelPalpitar(liga.Id, model);

            return View(viewModel);
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("CriticalActions")]
        public async Task<IActionResult> Palpitar(PalpitarLigaViewModel model)
        {
            _logger.LogInformation(
                "Iniciando envio de palpites. LigaId: {LigaId}. LigaParticipanteId: {LigaParticipanteId}",
                model.LigaId,
                model.LigaParticipanteId);

            try
            {
                return await SalvarPalpitesAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao salvar palpites. LigaId: {LigaId}. LigaParticipanteId: {LigaParticipanteId}",
                    model.LigaId,
                    model.LigaParticipanteId);

                throw;
            }
        }

        private async Task<IActionResult> SalvarPalpitesAsync(PalpitarLigaViewModel model)
        {
            if (model.LigaParticipanteId <= 0)
            {
                if (model.ParticipanteBloqueado || string.Equals(model.Origem, "minhasligas", StringComparison.OrdinalIgnoreCase))
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    var participanteDoUsuario = await _context.LigaParticipantes
                        .FirstOrDefaultAsync(p =>
                            p.Id == model.LigaParticipanteId &&
                            p.LigaId == model.LigaId &&
                            p.UserId == userId &&
                            p.Ativo);

                    if (participanteDoUsuario == null)
                    {
                        ModelState.AddModelError("", "Você não tem permissão para enviar palpites por este participante.");
                    }

                    model.ParticipanteBloqueado = true;
                    model.Origem = "minhasligas";
                    model.NomeParticipanteSelecionado = participanteDoUsuario?.Nome;
                }
            }

            var participanteExiste = await _context.LigaParticipantes
                .AnyAsync(p =>
                    p.Id == model.LigaParticipanteId &&
                    p.LigaId == model.LigaId &&
                    p.Ativo);

            if (!participanteExiste)
            {
                ModelState.AddModelError("LigaParticipanteId", "Participante inválido para esta liga.");
            }

            if (!ModelState.IsValid)
            {
                var viewModelErro = await MontarViewModelPalpitar(model.LigaId, model);
                return View(viewModelErro);
            }

            foreach (var item in model.Jogos)
            {
                if (!item.GolsCasaPalpite.HasValue || !item.GolsVisitantePalpite.HasValue)
                {
                    continue;
                }

                var jogo = await _context.Jogos.FindAsync(item.JogoId);

                if (jogo == null)
                {
                    continue;
                }

                if (jogo.DataJogo <= DateTime.Now.AddMinutes(MinutosBloqueioPalpite))
                {
                    continue;
                }

                var palpiteExistente = await _context.Palpites
                    .FirstOrDefaultAsync(p =>
                        p.LigaId == model.LigaId &&
                        p.LigaParticipanteId == model.LigaParticipanteId &&
                        p.JogoId == item.JogoId);

                if (palpiteExistente == null)
                {
                    var novoPalpite = new Palpite
                    {
                        LigaId = model.LigaId,
                        LigaParticipanteId = model.LigaParticipanteId,
                        JogoId = item.JogoId,
                        GolsCasaPalpite = item.GolsCasaPalpite.Value,
                        GolsVisitantePalpite = item.GolsVisitantePalpite.Value,
                        DataPalpite = DateTime.Now,
                        PontosGanhos = 0,
                        Ativo = true
                    };

                    _context.Palpites.Add(novoPalpite);
                }
                else
                {
                    palpiteExistente.GolsCasaPalpite = item.GolsCasaPalpite.Value;
                    palpiteExistente.GolsVisitantePalpite = item.GolsVisitantePalpite.Value;
                    palpiteExistente.DataPalpite = DateTime.Now;
                    palpiteExistente.Ativo = true;

                    _context.Palpites.Update(palpiteExistente);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Palpites salvos com sucesso. LigaId: {LigaId}. LigaParticipanteId: {LigaParticipanteId}",
                model.LigaId,
                model.LigaParticipanteId);

            TempData["Sucesso"] = "Palpites salvos com sucesso.";

            return RedirectToAction(nameof(Palpitar), new
            {
                id = model.LigaId,
                participanteId = model.LigaParticipanteId,
                origem = model.Origem
            });
        }

        private async Task<PalpitarLigaViewModel> MontarViewModelPalpitar(
    int ligaId,
    PalpitarLigaViewModel? modelPostado = null)
        {
            var liga = await _context.Ligas
                .Include(l => l.Campeonato)
                .FirstOrDefaultAsync(l => l.Id == ligaId);

            if (liga == null)
            {
                return new PalpitarLigaViewModel();
            }

            var participantes = await _context.LigaParticipantes
                .Where(p => p.LigaId == ligaId && p.Ativo)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            var jogos = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.CampeonatoId == liga.CampeonatoId &&
                    j.Ativo)
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            var participanteSelecionado = modelPostado?.LigaParticipanteId ?? 0;

            var viewModel = new PalpitarLigaViewModel
            {
                LigaId = liga.Id,
                NomeLiga = liga.Nome,
                NomeCampeonato = liga.Campeonato?.Nome ?? "",
                LigaParticipanteId = participanteSelecionado,
                Participantes = participantes.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Nome,
                    Selected = p.Id == participanteSelecionado
                }).ToList()
            };

            foreach (var jogo in jogos)
            {
                var palpiteExistente = participanteSelecionado > 0
                    ? await _context.Palpites.FirstOrDefaultAsync(p =>
                        p.LigaId == ligaId &&
                        p.LigaParticipanteId == participanteSelecionado &&
                        p.JogoId == jogo.Id)
                    : null;

                var jogoPostado = modelPostado?.Jogos
                    .FirstOrDefault(j => j.JogoId == jogo.Id);

                viewModel.Jogos.Add(new PalpiteJogoViewModel
                {
                    JogoId = jogo.Id,
                    TimeCasa = jogo.TimeCasa?.Nome ?? "",
                    TimeVisitante = jogo.TimeVisitante?.Nome ?? "",
                    DataJogo = jogo.DataJogo,
                    Fase = jogo.Fase,
                    Grupo = jogo.Grupo,
                    GolsCasaPalpite = jogoPostado?.GolsCasaPalpite ?? palpiteExistente?.GolsCasaPalpite,
                    GolsVisitantePalpite = jogoPostado?.GolsVisitantePalpite ?? palpiteExistente?.GolsVisitantePalpite,
                    JaPalpitado = palpiteExistente != null,
                    Bloqueado = jogo.DataJogo <= DateTime.Now.AddMinutes(MinutosBloqueioPalpite)
                });
            }

            return viewModel;
        }

        [Authorize(Roles = AppRoles.Administrador)]
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

        [Authorize(Roles = AppRoles.Administrador)]
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

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
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

        [Authorize(Roles = AppRoles.Administrador)]
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

        [Authorize(Roles = AppRoles.Administrador)]
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
