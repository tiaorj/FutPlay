using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        // Catálogo público de ligas (apenas ligas Ativas e Públicas)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var ligas = await _context.Ligas
                .Include(l => l.Campeonato)
                .Where(l => l.Ativo && l.Publica)
                .OrderBy(l => l.Nome)
                .ToListAsync();

            var lista = new List<LigaCatalogoItemViewModel>();

            foreach (var liga in ligas)
            {
                bool participa = false;

                if (User?.Identity?.IsAuthenticated == true)
                {
                    var participante = await ObterParticipanteUsuarioAsync(liga.Id, vincularPorEmail: true);
                    participa = participante != null;
                }

                lista.Add(new LigaCatalogoItemViewModel
                {
                    Liga = liga,
                    UsuarioParticipa = participa
                });
            }

            return View(lista);
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

            if (!UsuarioEhAdministrador())
            {
                var participanteUsuario = await ObterParticipanteUsuarioAsync(liga.Id, vincularPorEmail: true);
                if (participanteUsuario == null)
                {
                    TempData["Erro"] = "Não encontramos um participante vinculado ao seu usuário nesta liga.";
                    return RedirectToAction("Index", "MinhasLigas");
                }

                var modelUsuario = new PalpitarLigaViewModel
                {
                    LigaParticipanteId = participanteUsuario.Id,
                    ParticipanteBloqueado = true,
                    NomeParticipanteSelecionado = participanteUsuario.Nome,
                    Origem = "minhasligas"
                };

                var viewModelUsuario = await MontarViewModelPalpitar(liga.Id, modelUsuario);

                return View(viewModelUsuario);
            }

            if (veioDeMinhasLigas)
            {
                var participanteUsuario = await ObterParticipanteUsuarioAsync(liga.Id, vincularPorEmail: true);
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
            if (!UsuarioEhAdministrador())
            {
                var participanteUsuario = await ObterParticipanteUsuarioAsync(model.LigaId, vincularPorEmail: true);

                if (participanteUsuario == null)
                {
                    ModelState.AddModelError("", "Você não tem um participante vinculado ao seu usuário nesta liga.");
                }
                else
                {
                    model.LigaParticipanteId = participanteUsuario.Id;
                    model.ParticipanteBloqueado = true;
                    model.Origem = "minhasligas";
                    model.NomeParticipanteSelecionado = participanteUsuario.Nome;
                }
            }

            if (model.LigaParticipanteId <= 0)
            {
                if (model.ParticipanteBloqueado || string.Equals(model.Origem, "minhasligas", StringComparison.OrdinalIgnoreCase))
                {
                    var participanteDoUsuario = await ObterParticipanteUsuarioAsync(model.LigaId, vincularPorEmail: true);

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


            var totalSalvos = 0;
            var totalIgnorados = 0;

            foreach (var item in model.Jogos)
            {
                var temCasa = item.GolsCasaPalpite.HasValue;
                var temVisitante = item.GolsVisitantePalpite.HasValue;

                if (!temCasa && !temVisitante)
                {
                    continue;
                }

                if (temCasa != temVisitante)
                {
                    totalIgnorados++;
                    ModelState.AddModelError("", "Preencha os dois placares do jogo ou deixe os dois vazios.");
                    continue;
                }

                var jogo = await _context.Jogos.FindAsync(item.JogoId);

                if (jogo == null)
                {
                    totalIgnorados++;
                    continue;
                }

                if (jogo.DataJogo <= DateTime.Now.AddMinutes(MinutosBloqueioPalpite))
                {
                    totalIgnorados++;
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
                        GolsCasaPalpite = item.GolsCasaPalpite!.Value,
                        GolsVisitantePalpite = item.GolsVisitantePalpite!.Value,
                        DataPalpite = DateTime.Now,
                        PontosGanhos = 0,
                        Ativo = true
                    };

                    _context.Palpites.Add(novoPalpite);
                }
                else
                {
                    palpiteExistente.GolsCasaPalpite = item.GolsCasaPalpite!.Value;
                    palpiteExistente.GolsVisitantePalpite = item.GolsVisitantePalpite!.Value;
                    palpiteExistente.DataPalpite = DateTime.Now;
                    palpiteExistente.Ativo = true;

                    _context.Palpites.Update(palpiteExistente);
                }

                totalSalvos++;
            }

            if (!ModelState.IsValid)
            {
                var viewModelErro = await MontarViewModelPalpitar(model.LigaId, model);
                return View(viewModelErro);
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"{totalSalvos} palpite(s) salvo(s) com sucesso.";

            if (totalIgnorados > 0)
            {
                TempData["Erro"] = $"{totalIgnorados} jogo(s) não foram salvos por estarem incompletos, bloqueados ou inválidos.";
            }

            if (!ModelState.IsValid)
            {
                var viewModelErro = await MontarViewModelPalpitar(model.LigaId, model);
                return View(viewModelErro);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Palpites salvos com sucesso. LigaId: {LigaId}. LigaParticipanteId: {LigaParticipanteId}",
                model.LigaId,
                model.LigaParticipanteId);

            TempData["Sucesso"] = $"{totalSalvos} palpite(s) salvo(s) com sucesso.";

            if (totalIgnorados > 0)
            {
                TempData["Erro"] = $"{totalIgnorados} jogo(s) não foram salvos por estarem incompletos, bloqueados ou inválidos.";
            }

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
                ParticipanteBloqueado = modelPostado?.ParticipanteBloqueado ?? false,
                NomeParticipanteSelecionado = modelPostado?.NomeParticipanteSelecionado,
                Origem = modelPostado?.Origem,
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
                    TimeCasaEscudoUrl = jogo.TimeCasa?.EscudoUrl,
                    TimeVisitante = jogo.TimeVisitante?.Nome ?? "",
                    TimeVisitanteEscudoUrl = jogo.TimeVisitante?.EscudoUrl,
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

        private bool UsuarioEhAdministrador()
        {
            return User.IsInRole(AppRoles.Administrador);
        }

        private async Task<LigaParticipante?> ObterParticipanteUsuarioAsync(
            int ligaId,
            bool vincularPorEmail)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var emailNormalizado = email?.ToUpper();

            var participante = await _context.LigaParticipantes
                .FirstOrDefaultAsync(p =>
                    p.LigaId == ligaId &&
                    p.Ativo &&
                    ((userId != null && p.UserId == userId) ||
                     (emailNormalizado != null && p.Email.ToUpper() == emailNormalizado)));

            if (participante != null &&
                vincularPorEmail &&
                participante.UserId == null &&
                !string.IsNullOrWhiteSpace(userId))
            {
                participante.UserId = userId;
                await _context.SaveChangesAsync();
            }

            return participante;
        }


        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        public async Task<IActionResult> Create()
        {
            await CarregarCampeonatos();

            return View(new Liga
            {
                CodigoConvite = GerarCodigoConvite(),
                DataCriacao = DateTime.Now,
                Ativo = true,
                Publica = false
            });
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Liga liga)
        {
            if (string.IsNullOrWhiteSpace(liga.CodigoConvite))
            {
                liga.CodigoConvite = GerarCodigoConvite();
            }

            liga.CodigoConvite = liga.CodigoConvite.Trim().ToUpper();
            liga.DataCriacao = DateTime.Now;
            liga.Ativo = true;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;

            liga.CriadorUserId = userId;

            if (ModelState.IsValid)
            {
                _context.Ligas.Add(liga);
                await _context.SaveChangesAsync();

                var jaParticipa = await _context.LigaParticipantes
                    .AnyAsync(p =>
                        p.LigaId == liga.Id &&
                        p.Ativo &&
                        ((userId != null && p.UserId == userId) ||
                         (!string.IsNullOrWhiteSpace(email) && p.Email.ToUpper() == email.ToUpper())));

                if (!jaParticipa)
                {
                    var nome = User.Identity?.Name;

                    if (string.IsNullOrWhiteSpace(nome) && !string.IsNullOrWhiteSpace(email))
                    {
                        var atIdx = email.IndexOf('@');
                        nome = atIdx > 0 ? email.Substring(0, atIdx) : email;
                    }

                    var participante = new LigaParticipante
                    {
                        LigaId = liga.Id,
                        UserId = userId,
                        Nome = nome ?? email ?? "Participante",
                        Email = email ?? "",
                        DataEntrada = DateTime.Now,
                        PontuacaoTotal = 0,
                        Ativo = true
                    };

                    _context.LigaParticipantes.Add(participante);
                    await _context.SaveChangesAsync();
                }

                TempData["Sucesso"] = "Liga criada com sucesso. Compartilhe o convite com seus participantes.";

                return RedirectToAction(nameof(Convites), new { id = liga.Id });
            }

            await CarregarCampeonatos();
            return View(liga);
        }

        private bool UsuarioPodeGerenciarLiga(Liga liga)
        {
            if (UsuarioEhAdministrador())
            {
                return true;
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(userId) &&
                   !string.IsNullOrWhiteSpace(liga.CriadorUserId) &&
                   liga.CriadorUserId == userId;
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

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var liga = await _context.Ligas.FindAsync(id);

            if (liga == null)
                return NotFound();

            if (!UsuarioPodeGerenciarLiga(liga))
            {
                return Forbid();
            }

            await CarregarCampeonatos();
            return View(liga);
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Liga liga)
        {
            if (id != liga.Id)
                return NotFound();

            var ligaBanco = await _context.Ligas
                .FirstOrDefaultAsync(l => l.Id == id);

            if (ligaBanco == null)
                return NotFound();

            if (!UsuarioPodeGerenciarLiga(ligaBanco))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                ligaBanco.Nome = liga.Nome;
                ligaBanco.CampeonatoId = liga.CampeonatoId;
                ligaBanco.Publica = liga.Publica;
                ligaBanco.Ativo = liga.Ativo;

                if (UsuarioEhAdministrador())
                {
                    ligaBanco.CodigoConvite = string.IsNullOrWhiteSpace(liga.CodigoConvite)
                        ? ligaBanco.CodigoConvite
                        : liga.CodigoConvite.Trim().ToUpper();
                }

                _context.Update(ligaBanco);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Liga atualizada com sucesso.";

                return RedirectToAction("Index", "MinhasLigas");
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

        // Entrada em liga por código de convite
        [AllowAnonymous]
        public IActionResult Entrar(string? codigoConvite)
        {
            return View(new LigaEntrarViewModel
            {
                CodigoConvite = codigoConvite
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrar(LigaEntrarViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CodigoConvite))
            {
                ModelState.AddModelError("CodigoConvite", "Informe o código de convite.");
                return View(model);
            }

            var codigo = model.CodigoConvite.Trim().ToUpper();

            var liga = await _context.Ligas
                .FirstOrDefaultAsync(l => l.CodigoConvite.ToUpper() == codigo && l.Ativo);

            if (liga == null)
            {
                ModelState.AddModelError("", "Código inválido ou liga não encontrada.");
                return View(model);
            }

            // Verifica se o usuário já participa
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            var emailNormalizado = email?.ToUpper();

            var participanteExistente = await _context.LigaParticipantes
                .FirstOrDefaultAsync(p =>
                    p.LigaId == liga.Id &&
                    p.Ativo &&
                    ((userId != null && p.UserId == userId) ||
                     (emailNormalizado != null && p.Email.ToUpper() == emailNormalizado)));

            if (participanteExistente != null)
            {
                // já participa -> redireciona para MinhasLigas
                return RedirectToAction("Index", "MinhasLigas");
            }

            // Cria participante
            var nome = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(nome) && !string.IsNullOrWhiteSpace(email))
            {
                // usa a parte antes do @ como fallback
                var atIdx = email.IndexOf('@');
                nome = atIdx > 0 ? email.Substring(0, atIdx) : email;
            }

            var participante = new LigaParticipante
            {
                LigaId = liga.Id,
                UserId = userId,
                Nome = nome ?? email ?? "Participante",
                Email = email ?? "",
                DataEntrada = DateTime.Now,
                PontuacaoTotal = 0,
                Ativo = true
            };

            _context.LigaParticipantes.Add(participante);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "MinhasLigas");
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        public async Task<IActionResult> Convites(int? id)
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

            if (!UsuarioPodeGerenciarLiga(liga))
            {
                return Forbid();
            }

            var linkConvite = Url.Action(
                action: "Entrar",
                controller: "Ligas",
                values: new { codigoConvite = liga.CodigoConvite },
                protocol: Request.Scheme
            ) ?? string.Empty;

            var convites = await _context.LigaConvites
                .Where(c => c.LigaId == liga.Id)
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync();

            var viewModel = new LigaConviteViewModel
            {
                LigaId = liga.Id,
                Liga = liga,
                LinkConvite = linkConvite,
                Convites = convites
            };

            return View(viewModel);
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convidar(LigaConviteViewModel model)
        {
            ModelState.Remove("Liga");
            ModelState.Remove("Liga.Nome");
            ModelState.Remove("Liga.CodigoConvite");
            ModelState.Remove("Liga.CampeonatoId");
            ModelState.Remove("Liga.Campeonato");

            var liga = await _context.Ligas
                .Include(l => l.Campeonato)
                .FirstOrDefaultAsync(l => l.Id == model.LigaId);

            if (liga == null)
            {
                return NotFound();
            }

            if (!UsuarioPodeGerenciarLiga(liga))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.LigaId = liga.Id;
                model.Liga = liga;
                model.LinkConvite = Url.Action(
                    action: "Entrar",
                    controller: "Ligas",
                    values: new { codigoConvite = liga.CodigoConvite },
                    protocol: Request.Scheme
                ) ?? string.Empty;

                model.Convites = await _context.LigaConvites
                    .Where(c => c.LigaId == liga.Id)
                    .OrderByDescending(c => c.DataCriacao)
                    .ToListAsync();

                return View("Convites", model);
            }

            var emailNormalizado = model.Email.Trim().ToUpper();

            var jaExisteConvitePendente = await _context.LigaConvites
                .AnyAsync(c =>
                    c.LigaId == liga.Id &&
                    c.Ativo &&
                    c.Status == "Pendente" &&
                    c.Email.ToUpper() == emailNormalizado);

            if (jaExisteConvitePendente)
            {
                TempData["Erro"] = "Já existe um convite pendente para este e-mail.";
                return RedirectToAction(nameof(Convites), new { id = liga.Id });
            }

            var jaParticipa = await _context.LigaParticipantes
                .AnyAsync(p =>
                    p.LigaId == liga.Id &&
                    p.Ativo &&
                    p.Email.ToUpper() == emailNormalizado);

            if (jaParticipa)
            {
                TempData["Erro"] = "Este e-mail já participa da liga.";
                return RedirectToAction(nameof(Convites), new { id = liga.Id });
            }

            var convite = new LigaConvite
            {
                LigaId = liga.Id,
                Email = model.Email.Trim(),
                NomeConvidado = model.NomeConvidado?.Trim(),
                CodigoConvite = liga.CodigoConvite,
                TokenConvite = Guid.NewGuid().ToString("N"),
                Status = "Pendente",
                DataCriacao = DateTime.Now,
                Ativo = true
            };

            _context.LigaConvites.Add(convite);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Convite criado com sucesso. Copie o link e envie para o convidado.";

            return RedirectToAction(nameof(Convites), new { id = liga.Id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> AceitarConvite(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Erro"] = "Convite inválido.";
                return RedirectToAction(nameof(Index));
            }

            var convite = await _context.LigaConvites
                .Include(c => c.Liga)
                .FirstOrDefaultAsync(c =>
                    c.TokenConvite == token &&
                    c.Ativo);

            if (convite == null)
            {
                TempData["Erro"] = "Convite inválido, cancelado ou inexistente.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(convite.Status, "Pendente", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Erro"] = $"Este convite não está mais disponível. Status atual: {convite.Status}.";
                return RedirectToAction(nameof(Index));
            }

            if (convite.Liga == null || !convite.Liga.Ativo)
            {
                TempData["Erro"] = "A liga deste convite não está mais disponível.";
                return RedirectToAction(nameof(Index));
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action(
                    action: nameof(AceitarConvite),
                    controller: "Ligas",
                    values: new { token }
                );

                returnUrl ??= $"/Ligas/AceitarConvite?token={Uri.EscapeDataString(token)}";

                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity.Name;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Erro"] = "Não foi possível identificar o e-mail do usuário logado.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(email, convite.Email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Erro"] = "Este convite foi enviado para outro e-mail. Entre com o e-mail correto para aceitar o convite.";
                return RedirectToAction(nameof(Index));
            }

            var jaParticipa = await _context.LigaParticipantes
                .AnyAsync(p =>
                    p.LigaId == convite.LigaId &&
                    p.Ativo &&
                    ((userId != null && p.UserId == userId) ||
                     p.Email.ToUpper() == email.ToUpper()));

            if (!jaParticipa)
            {
                var nome = User.Identity?.Name;

                if (string.IsNullOrWhiteSpace(nome))
                {
                    var atIdx = email.IndexOf('@');
                    nome = atIdx > 0 ? email.Substring(0, atIdx) : email;
                }

                var participante = new LigaParticipante
                {
                    LigaId = convite.LigaId,
                    UserId = userId,
                    Nome = string.IsNullOrWhiteSpace(convite.NomeConvidado) ? nome! : convite.NomeConvidado,
                    Email = email,
                    DataEntrada = DateTime.Now,
                    PontuacaoTotal = 0,
                    Ativo = true
                };

                _context.LigaParticipantes.Add(participante);
            }

            convite.Status = "Aceito";
            convite.DataAceite = DateTime.Now;
            convite.UserIdAceite = userId;

            _context.LigaConvites.Update(convite);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Você entrou na liga com sucesso.";

            return RedirectToAction("Index", "MinhasLigas");
        }

        [Authorize(Roles = AppRoles.AdministradorOuParticipante)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarConvite(int id)
        {
            var convite = await _context.LigaConvites
                .Include(c => c.Liga)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (convite == null || convite.Liga == null)
            {
                return NotFound();
            }

            if (!UsuarioPodeGerenciarLiga(convite.Liga))
            {
                return Forbid();
            }

            if (!string.Equals(convite.Status, "Pendente", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Erro"] = "Somente convites pendentes podem ser cancelados.";
                return RedirectToAction(nameof(Convites), new { id = convite.LigaId });
            }

            convite.Status = "Cancelado";
            convite.Ativo = false;

            _context.LigaConvites.Update(convite);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Convite cancelado com sucesso.";

            return RedirectToAction(nameof(Convites), new { id = convite.LigaId });
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

            // Segurança de acesso:
            // - Ranking de liga pública é público
            // - Ranking de liga privada só para Administrador ou participante vinculado
            if (!liga.Publica)
            {
                if (!UsuarioEhAdministrador())
                {
                    var participante = await ObterParticipanteUsuarioAsync(liga.Id, vincularPorEmail: true);

                    if (participante == null)
                    {
                        if (!User.Identity?.IsAuthenticated ?? true)
                        {
                            // visitante -> exigir login
                            return Challenge();
                        }

                        // usuário autenticado, mas não participante -> negar
                        return Forbid();
                    }
                }
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
