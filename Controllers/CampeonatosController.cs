using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FutPlay.Controllers
{
    public class CampeonatosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ClassificacaoService _classificacaoService;

        public CampeonatosController(
            AppDbContext context,
            ClassificacaoService classificacaoService)
        {
            _context = context;
            _classificacaoService = classificacaoService;
        }

        public async Task<IActionResult> Index(string filtro = "todos", string? pais = null, string? tipo = null)
        {
            filtro = NormalizarFiltro(filtro);

            var usuarioAutenticado = User.Identity?.IsAuthenticated == true;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var campeonatosFavoritosIds = new HashSet<int>();

            if (usuarioAutenticado && !string.IsNullOrWhiteSpace(userId))
            {
                campeonatosFavoritosIds = (await _context.CampeonatoFavoritos
                    .Where(f => f.UserId == userId)
                    .Select(f => f.CampeonatoId)
                    .ToListAsync())
                    .ToHashSet();
            }

            var todosCampeonatos = await _context.Campeonatos
                .OrderByDescending(c => c.Ativo)
                .ThenByDescending(c => c.Ano)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            IEnumerable<Campeonato> campeonatos = todosCampeonatos;

            campeonatos = filtro switch
            {
                "ativos" => campeonatos.Where(c => c.Ativo),
                "favoritos" => usuarioAutenticado
                    ? campeonatos.Where(c => campeonatosFavoritosIds.Contains(c.Id))
                    : Enumerable.Empty<Campeonato>(),
                _ => campeonatos
            };

            if (!string.IsNullOrWhiteSpace(pais))
            {
                campeonatos = campeonatos.Where(c => string.Equals(c.Pais, pais, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                campeonatos = campeonatos.Where(c => string.Equals(c.Tipo, tipo, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new CampeonatosIndexViewModel
            {
                Campeonatos = campeonatos
                    .OrderByDescending(c => c.Ativo)
                    .ThenByDescending(c => c.Ano)
                    .ThenBy(c => c.Nome)
                    .ToList(),
                Paises = todosCampeonatos
                    .Select(c => c.Pais)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p)
                    .ToList(),
                Tipos = todosCampeonatos
                    .Select(c => c.Tipo)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t)
                    .ToList(),
                CampeonatosFavoritosIds = campeonatosFavoritosIds,
                Filtro = filtro,
                Pais = pais,
                Tipo = tipo,
                UsuarioAutenticado = usuarioAutenticado,
                TotalCampeonatos = todosCampeonatos.Count,
                TotalAtivos = todosCampeonatos.Count(c => c.Ativo),
                TotalInativos = todosCampeonatos.Count(c => !c.Ativo),
                TotalPaises = todosCampeonatos
                    .Select(c => string.IsNullOrWhiteSpace(c.Pais) ? "Mundo" : c.Pais)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                TotalFavoritos = todosCampeonatos.Count(c => campeonatosFavoritosIds.Contains(c.Id))
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlternarFavorito(int id, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["Erro"] = "Entre na sua conta para favoritar campeonatos.";
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

            var campeonatoExiste = await _context.Campeonatos.AnyAsync(c => c.Id == id);

            if (!campeonatoExiste)
            {
                return NotFound();
            }

            var favorito = await _context.CampeonatoFavoritos
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CampeonatoId == id);

            if (favorito == null)
            {
                _context.CampeonatoFavoritos.Add(new CampeonatoFavorito
                {
                    UserId = userId,
                    CampeonatoId = id
                });

                TempData["Sucesso"] = "Campeonato adicionado aos favoritos.";
            }
            else
            {
                _context.CampeonatoFavoritos.Remove(favorito);
                TempData["Sucesso"] = "Campeonato removido dos favoritos.";
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
        public async Task<IActionResult> Create(Campeonato campeonato)
        {
            ValidarFormato(campeonato);

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

        [Authorize(Roles = AppRoles.Administrador)]
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

        [Authorize(Roles = AppRoles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Campeonato campeonato)
        {
            if (id != campeonato.Id)
            {
                return NotFound();
            }

            ValidarFormato(campeonato);

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

        [Authorize(Roles = AppRoles.Administrador)]
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

        [Authorize(Roles = AppRoles.Administrador)]
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

            if (campeonato.UsaClassificacaoPorGrupos)
            {
                await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(id.Value);
            }

            var classificacoes = await _context.Classificacoes
                .Include(c => c.Time)
                .Where(c => c.CampeonatoId == id && c.Ativo)
                .OrderBy(c => c.Posicao)
                .ToListAsync();

            var grupos = campeonato.UsaClassificacaoPorGrupos
                ? await _context.Grupos
                    .Where(g => g.CampeonatoId == id && g.Ativo)
                    .OrderBy(g => g.Nome)
                    .ToListAsync()
                : new List<Grupo>();

            var campeonatoTimes = campeonato.UsaClassificacaoPorGrupos
                ? await _context.CampeonatoTimes
                    .Include(ct => ct.Time)
                    .Include(ct => ct.Grupo)
                    .Where(ct => ct.CampeonatoId == id && ct.Ativo)
                    .OrderBy(ct => ct.Grupo != null ? ct.Grupo.Nome : string.Empty)
                    .ThenBy(ct => ct.Time != null ? ct.Time.Nome : string.Empty)
                    .ToListAsync()
                : new List<CampeonatoTime>();

            var viewModel = new ClassificacaoCampeonatoViewModel
            {
                Campeonato = campeonato,
                Classificacoes = classificacoes,
                Grupos = grupos,
                CampeonatoTimes = campeonatoTimes,
                UltimosResultadosPorTime = await ObterUltimosResultadosPorTimeAsync(id.Value)
            };

            return View(viewModel);
        }

        [Authorize(Roles = AppRoles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalcularClassificacao(int id)
        {
            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campeonato == null)
            {
                return NotFound();
            }

            await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(id);

            TempData["Sucesso"] = "Classificação recalculada com sucesso.";

            return RedirectToAction(nameof(Portal), new { id, aba = "classificacao" });
        }

        public async Task<IActionResult> Portal(
            int? id,
            string aba = "visao-geral",
            string? modo = null,
            string? dataSelecionada = null,
            int? rodadaSelecionada = null,
            string? grupoSelecionado = null,
            string? faseSelecionada = null,
            int? timeDestaqueId = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            aba = NormalizarAbaPortal(aba);
            var hoje = DateTime.Today;

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campeonato == null)
            {
                return NotFound();
            }

            if (campeonato.UsaClassificacaoPorGrupos && aba == "classificacao")
            {
                await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(id.Value);
            }

            modo = NormalizarModoPortal(modo, campeonato.UsaClassificacaoPorGrupos, !campeonato.UsaClassificacaoPorGrupos);
            grupoSelecionado = string.IsNullOrWhiteSpace(grupoSelecionado)
                ? null
                : grupoSelecionado.Trim();
            faseSelecionada = string.IsNullOrWhiteSpace(faseSelecionada)
                ? null
                : faseSelecionada.Trim();

            var classificacoes = await _context.Classificacoes
                .Include(c => c.Time)
                .Where(c => c.CampeonatoId == id && c.Ativo)
                .OrderBy(c => c.Posicao)
                .ToListAsync();

            var grupos = campeonato.UsaClassificacaoPorGrupos
                ? await _context.Grupos
                    .Where(g => g.CampeonatoId == id && g.Ativo)
                    .OrderBy(g => g.Nome)
                    .ToListAsync()
                : new List<Grupo>();

            var campeonatoTimes = campeonato.UsaClassificacaoPorGrupos
                ? await _context.CampeonatoTimes
                    .Include(ct => ct.Time)
                    .Include(ct => ct.Grupo)
                    .Where(ct => ct.CampeonatoId == id && ct.Ativo)
                    .OrderBy(ct => ct.Grupo != null ? ct.Grupo.Nome : string.Empty)
                    .ThenBy(ct => ct.Time != null ? ct.Time.Nome : string.Empty)
                    .ToListAsync()
                : new List<CampeonatoTime>();

            var jogosCampeonato = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.CampeonatoId == id &&
                    j.Ativo)
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

            var gruposPorTime = campeonatoTimes
                .Where(ct => ct.Grupo != null && !string.IsNullOrWhiteSpace(ct.Grupo.Nome))
                .GroupBy(ct => ct.TimeId)
                .ToDictionary(g => g.Key, g => g.First().Grupo!.Nome.Trim());

            string? ObterGrupoJogo(Jogo jogo)
            {
                if (!string.IsNullOrWhiteSpace(jogo.Grupo))
                {
                    return jogo.Grupo.Trim();
                }

                gruposPorTime.TryGetValue(jogo.TimeCasaId, out var grupoCasa);
                gruposPorTime.TryGetValue(jogo.TimeVisitanteId, out var grupoVisitante);

                if (!string.IsNullOrWhiteSpace(grupoCasa) &&
                    (string.IsNullOrWhiteSpace(grupoVisitante) ||
                     string.Equals(grupoCasa, grupoVisitante, StringComparison.OrdinalIgnoreCase)))
                {
                    return grupoCasa;
                }

                return !string.IsNullOrWhiteSpace(grupoVisitante)
                    ? grupoVisitante
                    : null;
            }

            // Rodadas: mantive compatibilidade, usando o parâmetro 'rodadaSelecionada' (query string 'rodadaSelecionada')
            var rodadas = ObterRodadas(
                jogosCampeonato,
                hoje,
                rodadaSelecionada,
                out var rodadaSelecionadaOut,
                out var rodadaAnterior,
                out var proximaRodada);

            // Datas disponíveis para modo=data
            var datas = jogosCampeonato
                .GroupBy(j => j.DataJogo.Date)
                .Select(g => new DataFiltroViewModel
                {
                    Data = g.Key.Date,
                    TotalJogos = g.Count(),
                    Selecionada = !string.IsNullOrWhiteSpace(dataSelecionada) && DateTime.TryParse(dataSelecionada, out var d) && d.Date == g.Key
                })
                .OrderBy(d => d.Data)
                .ToList();

            var fases = jogosCampeonato
                .Select(j => NomeFasePortal(j.Fase))
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .GroupBy(f => f)
                .Select(g => new FaseFiltroViewModel
                {
                    Nome = g.Key,
                    TotalJogos = jogosCampeonato.Count(j => string.Equals(NomeFasePortal(j.Fase), g.Key, StringComparison.OrdinalIgnoreCase))
                })
                .OrderBy(f => OrdemFasePortal(f.Nome))
                .ThenBy(f => f.Nome)
                .ToList();

            if (string.Equals(modo, "fase", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(faseSelecionada) && fases.Any())
                {
                    faseSelecionada = fases.Last().Nome;
                }

                if (!string.IsNullOrWhiteSpace(faseSelecionada) &&
                    !fases.Any(f => string.Equals(f.Nome, faseSelecionada, StringComparison.OrdinalIgnoreCase)))
                {
                    faseSelecionada = fases.LastOrDefault()?.Nome;
                }
            }

            foreach (var fase in fases)
            {
                fase.Selecionada = string.Equals(fase.Nome, faseSelecionada, StringComparison.OrdinalIgnoreCase);
            }

            var fasesOrdenadas = fases.Select(f => f.Nome).ToList();
            string? faseAnterior = null;
            string? proximaFase = null;

            if (!string.IsNullOrWhiteSpace(faseSelecionada))
            {
                var faseIndex = fasesOrdenadas.FindIndex(f => string.Equals(f, faseSelecionada, StringComparison.OrdinalIgnoreCase));

                if (faseIndex > 0)
                {
                    faseAnterior = fasesOrdenadas[faseIndex - 1];
                }

                if (faseIndex >= 0 && faseIndex < fasesOrdenadas.Count - 1)
                {
                    proximaFase = fasesOrdenadas[faseIndex + 1];
                }
            }

            var seriesChaveamento = MontarSeriesChaveamento(jogosCampeonato);
            var faseChaveamentoSelecionada = faseSelecionada ?? fases.LastOrDefault()?.Nome;
            var chaveamentoFases = MontarFasesChaveamento(seriesChaveamento, faseChaveamentoSelecionada);
            var serieJogoLabels = seriesChaveamento
                .SelectMany(serie => serie.Jogos.Select(jogo => new
                {
                    jogo.JogoId,
                    Label = $"{serie.TimeANome} x {serie.TimeBNome} - {jogo.Ordem}"
                }))
                .GroupBy(item => item.JogoId)
                .ToDictionary(g => g.Key, g => g.First().Label);

            // Lógica para escolher quais jogos exibir
            List<Jogo> jogosDaRodada;
            if (string.Equals(modo, "data", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(dataSelecionada) && DateTime.TryParse(dataSelecionada, out var dataSel))
                {
                    jogosDaRodada = jogosCampeonato.Where(j => j.DataJogo.Date == dataSel.Date).OrderBy(j => j.DataJogo).ToList();
                }
                else
                {
                    jogosDaRodada = jogosCampeonato;
                }
            }
            else if (string.Equals(modo, "grupo", StringComparison.OrdinalIgnoreCase))
            {
                jogosDaRodada = string.IsNullOrWhiteSpace(grupoSelecionado)
                    ? jogosCampeonato
                        .OrderBy(j => ObterGrupoJogo(j) ?? "ZZZ")
                        .ThenBy(j => j.DataJogo)
                        .ToList()
                    : jogosCampeonato
                        .Where(j => string.Equals(ObterGrupoJogo(j), grupoSelecionado, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(j => j.DataJogo)
                        .ToList();
            }
            else if (string.Equals(modo, "fase", StringComparison.OrdinalIgnoreCase))
            {
                jogosDaRodada = string.IsNullOrWhiteSpace(faseSelecionada)
                    ? jogosCampeonato
                        .OrderBy(j => OrdemFasePortal(NomeFasePortal(j.Fase)))
                        .ThenBy(j => j.DataJogo)
                        .ToList()
                    : jogosCampeonato
                        .Where(j => string.Equals(NomeFasePortal(j.Fase), faseSelecionada, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(j => j.DataJogo)
                        .ToList();
            }
            else // modo=rodada (comportamento anterior)
            {
                jogosDaRodada = rodadaSelecionadaOut.HasValue && rodadas.Any(r => r.Rodada == rodadaSelecionadaOut.Value)
                    ? jogosCampeonato.Where(j => j.Rodada == rodadaSelecionadaOut.Value).OrderBy(j => j.DataJogo).ToList()
                    : jogosCampeonato;
            }

            var proximosJogos = jogosCampeonato
                .Where(j => j.DataJogo >= DateTime.Now && !EhFinalizado(j))
                .OrderBy(j => j.DataJogo)
                .ToList();

            var jogosFinalizados = jogosCampeonato
                .Where(EhFinalizado)
                .OrderByDescending(j => j.DataJogo)
                .ToList();

            var viewModel = new PortalCampeonatoViewModel
            {
                Campeonato = campeonato,
                Classificacoes = classificacoes,
                Grupos = grupos,
                CampeonatoTimes = campeonatoTimes,
                Jogos = jogosCampeonato,
                JogosDaRodada = jogosDaRodada,
                ProximosJogos = proximosJogos,
                JogosFinalizados = jogosFinalizados,
                Rodadas = rodadas,
                Aba = aba,
                RodadaSelecionada = rodadaSelecionadaOut,
                RodadaAnterior = rodadaAnterior,
                ProximaRodada = proximaRodada,
                TotalJogos = jogosCampeonato.Count,
                TotalHoje = jogosCampeonato.Count(j => j.DataJogo.Date == hoje),
                TotalProximos = jogosCampeonato.Count(j => EhProximo(j, hoje)),
                TotalFinalizados = jogosCampeonato.Count(EhFinalizado),
                UltimosResultadosPorTime = await ObterUltimosResultadosPorTimeAsync(id.Value),
                // novas props
                Datas = datas,
                Fases = fases,
                ChaveamentoFases = chaveamentoFases,
                SerieJogoLabels = serieJogoLabels,
                Modo = modo,
                GrupoSelecionado = grupoSelecionado,
                FaseSelecionada = faseSelecionada,
                FaseAnterior = faseAnterior,
                ProximaFase = proximaFase,
                TimeDestaqueId = timeDestaqueId,
                DataSelecionada = dataSelecionada
            };

            return View(viewModel);
        }

        private static bool EhFaseEliminatoria(Jogo jogo)
        {
            var faseNormalizada = NomeFasePortal(jogo.Fase);
            var ordem = OrdemFasePortal(faseNormalizada);

            return ordem >= 2 &&
                ordem <= 6 &&
                !faseNormalizada.Contains("grupo", StringComparison.OrdinalIgnoreCase);
        }

        private static List<ChaveamentoSerieViewModel> MontarSeriesChaveamento(List<Jogo> jogos)
        {
            return jogos
                .Where(EhFaseEliminatoria)
                .GroupBy(j =>
                {
                    var timeMenorId = Math.Min(j.TimeCasaId, j.TimeVisitanteId);
                    var timeMaiorId = Math.Max(j.TimeCasaId, j.TimeVisitanteId);

                    return new
                    {
                        Fase = NomeFasePortal(j.Fase),
                        TimeMenorId = timeMenorId,
                        TimeMaiorId = timeMaiorId
                    };
                })
                .Select(g => MontarSerieChaveamento(g.Key.Fase, g.OrderBy(j => j.DataJogo).ToList()))
                .OrderBy(s => OrdemFasePortal(s.Fase))
                .ThenBy(s => s.TimeANome)
                .ThenBy(s => s.TimeBNome)
                .ToList();
        }

        private static ChaveamentoSerieViewModel MontarSerieChaveamento(string fase, List<Jogo> jogos)
        {
            var primeiroJogo = jogos.First();
            var timeAId = primeiroJogo.TimeCasaId;
            var timeBId = primeiroJogo.TimeVisitanteId;

            var jogosDaSerie = jogos
                .Select((jogo, index) => MontarJogoSerie(jogo, index, timeAId, timeBId))
                .ToList();

            var jogosComPlacar = jogosDaSerie
                .Where(j => j.GolsTimeA.HasValue && j.GolsTimeB.HasValue)
                .ToList();

            int? agregadoA = jogosComPlacar.Any()
                ? jogosComPlacar.Sum(j => j.GolsTimeA.GetValueOrDefault())
                : null;

            int? agregadoB = jogosComPlacar.Any()
                ? jogosComPlacar.Sum(j => j.GolsTimeB.GetValueOrDefault())
                : null;

            var serieCompleta = jogosDaSerie.Any() &&
                jogosDaSerie.All(j => j.GolsTimeA.HasValue && j.GolsTimeB.HasValue);
            int? classificadoId = null;

            if (serieCompleta && agregadoA.HasValue && agregadoB.HasValue && agregadoA != agregadoB)
            {
                classificadoId = agregadoA > agregadoB ? timeAId : timeBId;
            }

            return new ChaveamentoSerieViewModel
            {
                Fase = fase,
                TimeAId = timeAId,
                TimeANome = primeiroJogo.TimeCasa?.Nome ?? "Mandante",
                TimeAEscudoUrl = primeiroJogo.TimeCasa?.EscudoUrl,
                TimeASigla = ObterSiglaTime(primeiroJogo.TimeCasa, "CAS"),
                TimeBId = timeBId,
                TimeBNome = primeiroJogo.TimeVisitante?.Nome ?? "Visitante",
                TimeBEscudoUrl = primeiroJogo.TimeVisitante?.EscudoUrl,
                TimeBSigla = ObterSiglaTime(primeiroJogo.TimeVisitante, "VIS"),
                AgregadoTimeA = agregadoA,
                AgregadoTimeB = agregadoB,
                ClassificadoTimeId = classificadoId,
                StatusSerie = classificadoId.HasValue
                    ? $"Classificado: {(classificadoId == timeAId ? primeiroJogo.TimeCasa?.Nome : primeiroJogo.TimeVisitante?.Nome)}"
                    : serieCompleta ? "Série indefinida" : "Série em aberto",
                Jogos = jogosDaSerie
            };
        }

        private static ChaveamentoJogoViewModel MontarJogoSerie(
            Jogo jogo,
            int index,
            int timeAId,
            int timeBId)
        {
            var golsTimeA = jogo.TimeCasaId == timeAId ? jogo.GolsCasa : jogo.GolsVisitante;
            var golsTimeB = jogo.TimeCasaId == timeBId ? jogo.GolsCasa : jogo.GolsVisitante;

            return new ChaveamentoJogoViewModel
            {
                JogoId = jogo.Id,
                DataJogo = jogo.DataJogo,
                Ordem = index == 0 ? "Ida" : index == 1 ? "Volta" : $"Jogo {index + 1}",
                MandanteId = jogo.TimeCasaId,
                MandanteNome = jogo.TimeCasa?.Nome ?? "Mandante",
                VisitanteId = jogo.TimeVisitanteId,
                VisitanteNome = jogo.TimeVisitante?.Nome ?? "Visitante",
                GolsMandante = jogo.GolsCasa,
                GolsVisitante = jogo.GolsVisitante,
                GolsTimeA = golsTimeA,
                GolsTimeB = golsTimeB
            };
        }

        private static List<ChaveamentoFaseViewModel> MontarFasesChaveamento(
            List<ChaveamentoSerieViewModel> series,
            string? faseSelecionada)
        {
            var fases = series
                .GroupBy(s => s.Fase)
                .Select(g => new ChaveamentoFaseViewModel
                {
                    Nome = g.Key,
                    Ordem = OrdemFasePortal(g.Key),
                    Series = g
                        .OrderBy(s => s.TimeANome)
                        .ThenBy(s => s.TimeBNome)
                        .ToList()
                })
                .OrderBy(f => f.Ordem)
                .ThenBy(f => f.Nome)
                .ToList();

            if (!fases.Any())
            {
                return fases;
            }

            var indiceSelecionado = string.IsNullOrWhiteSpace(faseSelecionada)
                ? fases.Count - 1
                : fases.FindIndex(f => string.Equals(f.Nome, faseSelecionada, StringComparison.OrdinalIgnoreCase));

            if (indiceSelecionado < 0)
            {
                indiceSelecionado = fases.Count - 1;
            }

            var indices = new SortedSet<int> { indiceSelecionado };

            if (indiceSelecionado > 0)
            {
                indices.Add(indiceSelecionado - 1);
            }

            if (indiceSelecionado < fases.Count - 1)
            {
                indices.Add(indiceSelecionado + 1);
            }

            return indices
                .Select(i =>
                {
                    var fase = fases[i];
                    fase.Selecionada = i == indiceSelecionado;
                    fase.PosicaoCss = i == indiceSelecionado
                        ? "is-selected"
                        : i < indiceSelecionado ? "is-before" : "is-after";

                    return fase;
                })
                .ToList();
        }

        private static string ObterSiglaTime(Time? time, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(time?.Sigla))
            {
                return time.Sigla;
            }

            if (!string.IsNullOrWhiteSpace(time?.Nome))
            {
                return time.Nome.Substring(0, Math.Min(3, time.Nome.Length)).ToUpperInvariant();
            }

            return fallback;
        }

        private static List<RodadaFiltroViewModel> ObterRodadas(
            List<Jogo> jogos,
            DateTime hoje,
            int? rodada,
            out int? rodadaSelecionada,
            out int? rodadaAnterior,
            out int? proximaRodada)
        {
            var rodadas = jogos
                .Where(j => j.Rodada.HasValue)
                .GroupBy(j => j.Rodada!.Value)
                .Select(g => new RodadaFiltroViewModel
                {
                    Rodada = g.Key,
                    DataReferencia = g.Min(j => j.DataJogo.Date),
                    TotalJogos = g.Count()
                })
                .OrderBy(r => r.Rodada)
                .ToList();

            rodadaSelecionada = rodada;

            var rodadaInformada = rodadaSelecionada;

            if (rodadaInformada.HasValue && !rodadas.Any(r => r.Rodada == rodadaInformada.Value))
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
            rodadaAnterior = null;
            proximaRodada = null;

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

            return rodadas;
        }

        private async Task<Dictionary<int, List<string>>> ObterUltimosResultadosPorTimeAsync(int campeonatoId)
        {
            var jogos = await _context.Jogos
                .Where(j =>
                    j.CampeonatoId == campeonatoId &&
                    j.Ativo &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue)
                .OrderByDescending(j => j.DataJogo)
                .Select(j => new
                {
                    j.TimeCasaId,
                    j.TimeVisitanteId,
                    j.GolsCasa,
                    j.GolsVisitante
                })
                .ToListAsync();

            var ultimosResultados = new Dictionary<int, List<string>>();

            foreach (var jogo in jogos)
            {
                var resultadoCasa = jogo.GolsCasa == jogo.GolsVisitante
                    ? "E"
                    : jogo.GolsCasa > jogo.GolsVisitante ? "V" : "D";

                var resultadoVisitante = jogo.GolsCasa == jogo.GolsVisitante
                    ? "E"
                    : jogo.GolsVisitante > jogo.GolsCasa ? "V" : "D";

                AdicionarResultado(ultimosResultados, jogo.TimeCasaId, resultadoCasa);
                AdicionarResultado(ultimosResultados, jogo.TimeVisitanteId, resultadoVisitante);
            }

            return ultimosResultados;
        }

        private static void AdicionarResultado(
            Dictionary<int, List<string>> ultimosResultados,
            int timeId,
            string resultado)
        {
            if (!ultimosResultados.TryGetValue(timeId, out var resultados))
            {
                resultados = new List<string>();
                ultimosResultados[timeId] = resultados;
            }

            if (resultados.Count < 5)
            {
                resultados.Add(resultado);
            }
        }

        private string ObterReturnUrlSeguro(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action(nameof(Index), "Campeonatos") ?? "/";
        }

        private void ValidarFormato(Campeonato campeonato)
        {
            if (string.IsNullOrWhiteSpace(campeonato.Formato))
            {
                campeonato.Formato = CampeonatoFormato.PontosCorridos;
                return;
            }

            if (!CampeonatoFormato.EhValido(campeonato.Formato))
            {
                ModelState.AddModelError(nameof(Campeonato.Formato), "Selecione um formato de disputa válido.");
                return;
            }

            campeonato.Formato = CampeonatoFormato.Normalizar(campeonato.Formato);
        }

        private static bool EhFinalizado(Jogo jogo)
        {
            return string.Equals(jogo.Status, "Finalizado", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EhProximo(Jogo jogo, DateTime hoje)
        {
            return jogo.DataJogo.Date >= hoje && !EhFinalizado(jogo);
        }

        private static string NormalizarFiltro(string? filtro)
        {
            return filtro?.ToLowerInvariant() switch
            {
                "ativos" => "ativos",
                "favoritos" => "favoritos",
                _ => "todos"
            };
        }

        private static string NormalizarAbaPortal(string? aba)
        {
            return aba?.ToLowerInvariant() switch
            {
                "jogos" => "jogos",
                "classificacao" => "classificacao",
                "fase-eliminatoria" => "fase-eliminatoria",
                "fase" => "fase-eliminatoria",
                "mata-mata" => "fase-eliminatoria",
                "estatisticas" => "estatisticas",
                "midia" => "midia",
                "visao" => "visao-geral",
                _ => "visao-geral"
            };
        }

        private static string NormalizarModoPortal(string? modo, bool permiteGrupo, bool preferirFase)
        {
            return modo?.ToLowerInvariant() switch
            {
                "data" => "data",
                "grupo" when permiteGrupo => "grupo",
                "fase" => "fase",
                "fases" => "fase",
                "mata-mata" => "fase",
                "rodada" => "rodada",
                _ => preferirFase ? "fase" : "rodada"
            };
        }

        private static string NomeFasePortal(string? fase)
        {
            if (string.IsNullOrWhiteSpace(fase))
            {
                return "Fase a definir";
            }

            var texto = fase.Trim();

            if (texto.Contains("final", StringComparison.OrdinalIgnoreCase))
            {
                if (texto.Contains("semi", StringComparison.OrdinalIgnoreCase))
                {
                    return "Semifinais";
                }

                if (texto.Contains("quarter", StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains("quarta", StringComparison.OrdinalIgnoreCase))
                {
                    return "Quartas de final";
                }

                if (texto.Contains("8th", StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains("oitava", StringComparison.OrdinalIgnoreCase))
                {
                    return "Oitavas-de-final";
                }

                return "Final";
            }

            if (texto.Contains("Round of 16", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16", StringComparison.OrdinalIgnoreCase))
            {
                return "Oitavas-de-final";
            }

            if (texto.Contains("Group", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("Grupo", StringComparison.OrdinalIgnoreCase))
            {
                return "Fase de grupos";
            }

            return texto;
        }

        private static int OrdemFasePortal(string? fase)
        {
            var texto = fase ?? string.Empty;

            if (texto.Contains("grupo", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (texto.Contains("32", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (texto.Contains("16", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("oitava", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (texto.Contains("quarta", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("quarter", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            if (texto.Contains("semi", StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }

            if (texto.Contains("final", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }

            return 20;
        }
    }
}
