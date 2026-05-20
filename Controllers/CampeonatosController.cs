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

            var classificacoes = await _context.Classificacoes
                .Include(c => c.Time)
                .Where(c => c.CampeonatoId == id && c.Ativo)
                .OrderBy(c => c.Grupo)
                .ThenBy(c => c.Posicao)
                .ToListAsync();

            var viewModel = new ClassificacaoCampeonatoViewModel
            {
                Campeonato = campeonato,
                Classificacoes = classificacoes,
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

        public async Task<IActionResult> Portal(int? id, string aba = "visao-geral", string modo = "rodada", string? dataSelecionada = null, int? rodadaSelecionada = null)
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

            var classificacoes = await _context.Classificacoes
                .Include(c => c.Time)
                .Where(c => c.CampeonatoId == id && c.Ativo)
                .OrderBy(c => c.Grupo)
                .ThenBy(c => c.Posicao)
                .ToListAsync();

            var jogosCampeonato = await _context.Jogos
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j =>
                    j.CampeonatoId == id &&
                    j.Ativo)
                .OrderBy(j => j.DataJogo)
                .ToListAsync();

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
                Modo = modo,
                DataSelecionada = dataSelecionada
            };

            return View(viewModel);
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
    }
}
