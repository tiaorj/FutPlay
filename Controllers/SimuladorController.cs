using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Controllers
{
    public class SimuladorController : Controller
    {
        private readonly AppDbContext _context;

        public SimuladorController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Simulador")]
        public async Task<IActionResult> Index()
        {
            var anoAtual = DateTime.Today.Year;

            var competicoes = await _context.Campeonatos
                .AsNoTracking()
                .Where(c => c.Ano == anoAtual && c.Ativo)
                .OrderByDescending(c => c.Ativo)
                .ThenBy(c => c.Nome)
                .Select(c => new SimuladorCompeticaoCardViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Ano = c.Ano,
                    Tipo = c.Tipo,
                    Formato = c.Formato,
                    FormatoDescricao = CampeonatoFormato.ObterDescricao(c.Formato),
                    Pais = PaisExibicao.Normalizar(c.Pais),
                    LogoUrl = c.LogoUrl
                })
                .ToListAsync();

            return View(new SimuladorIndexViewModel
            {
                Ano = anoAtual,
                Competicoes = competicoes
            });
        }

        [HttpGet("Simulador/Competicao/{campeonatoId:int}")]
        public async Task<IActionResult> Competicao(int campeonatoId)
        {
            var viewModel = await MontarViewModelAsync(campeonatoId, new Dictionary<int, (int? Casa, int? Visitante)>(), false);

            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        [HttpPost("Simulador/Competicao/{campeonatoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Competicao(int campeonatoId, SimuladorViewModel form)
        {
            var placares = form.Jogos
                .GroupBy(j => j.JogoId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var jogo = g.First();

                        return (NormalizarPlacar(jogo.PlacarCasa), NormalizarPlacar(jogo.PlacarVisitante));
                    });

            var viewModel = await MontarViewModelAsync(campeonatoId, placares, true);

            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        private async Task<SimuladorViewModel?> MontarViewModelAsync(
            int campeonatoId,
            Dictionary<int, (int? Casa, int? Visitante)> placares,
            bool simulado)
        {
            var campeonato = await _context.Campeonatos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                return null;
            }

            var grupos = campeonato.UsaClassificacaoPorGrupos
                ? await _context.Grupos
                    .AsNoTracking()
                    .Where(g => g.CampeonatoId == campeonatoId && g.Ativo)
                    .OrderBy(g => g.Nome)
                    .ToListAsync()
                : new List<Grupo>();

            var campeonatoTimes = campeonato.UsaClassificacaoPorGrupos
                ? await _context.CampeonatoTimes
                    .AsNoTracking()
                    .Include(ct => ct.Time)
                    .Include(ct => ct.Grupo)
                    .Where(ct => ct.CampeonatoId == campeonatoId && ct.Ativo)
                    .OrderBy(ct => ct.Grupo != null ? ct.Grupo.Nome : string.Empty)
                    .ThenBy(ct => ct.Time != null ? ct.Time.Nome : string.Empty)
                    .ToListAsync()
                : new List<CampeonatoTime>();

            var jogos = await _context.Jogos
                .AsNoTracking()
                .Include(j => j.TimeCasa)
                .Include(j => j.TimeVisitante)
                .Where(j => j.CampeonatoId == campeonatoId && j.Ativo)
                .ToListAsync();

            var gruposPorTime = campeonatoTimes
                .Where(ct => ct.Grupo != null && !string.IsNullOrWhiteSpace(ct.Grupo.Nome))
                .GroupBy(ct => ct.TimeId)
                .ToDictionary(g => g.Key, g => g.First().Grupo!.Nome.Trim());

            string? ObterGrupoJogo(Jogo jogo)
            {
                gruposPorTime.TryGetValue(jogo.TimeCasaId, out var grupoCasa);
                gruposPorTime.TryGetValue(jogo.TimeVisitanteId, out var grupoVisitante);

                if (!string.IsNullOrWhiteSpace(grupoCasa) &&
                    (string.IsNullOrWhiteSpace(grupoVisitante) ||
                     string.Equals(grupoCasa, grupoVisitante, StringComparison.OrdinalIgnoreCase)))
                {
                    return grupoCasa;
                }

                if (!string.IsNullOrWhiteSpace(grupoVisitante) && string.IsNullOrWhiteSpace(grupoCasa))
                {
                    return grupoVisitante;
                }

                return !string.IsNullOrWhiteSpace(jogo.Grupo)
                    ? jogo.Grupo.Trim()
                    : null;
            }

            var jogosViewModel = jogos
                .Select(j =>
                {
                    var grupo = campeonato.UsaClassificacaoPorGrupos ? ObterGrupoJogo(j) : null;
                    var temPlacarInformado = placares.TryGetValue(j.Id, out var placar);
                    var resultadoOficial = EhResultadoOficial(j);
                    var ehFaseDeGrupos = campeonato.UsaClassificacaoPorGrupos && EhJogoFaseGrupos(j, grupo);
                    var placarCasa = temPlacarInformado
                        ? placar.Casa
                        : resultadoOficial ? j.GolsCasa : null;
                    var placarVisitante = temPlacarInformado
                        ? placar.Visitante
                        : resultadoOficial ? j.GolsVisitante : null;

                    return new SimuladorJogoViewModel
                    {
                        JogoId = j.Id,
                        TimeCasaId = j.TimeCasaId,
                        TimeCasaNome = j.TimeCasa?.Nome ?? "Mandante",
                        TimeCasaEscudoUrl = j.TimeCasa?.EscudoUrl,
                        TimeCasaSigla = ObterSigla(j.TimeCasa, "CAS"),
                        TimeVisitanteId = j.TimeVisitanteId,
                        TimeVisitanteNome = j.TimeVisitante?.Nome ?? "Visitante",
                        TimeVisitanteEscudoUrl = j.TimeVisitante?.EscudoUrl,
                        TimeVisitanteSigla = ObterSigla(j.TimeVisitante, "VIS"),
                        DataJogo = j.DataJogo,
                        Rodada = j.Rodada,
                        Grupo = NomeGrupo(grupo),
                        GrupoChave = ChaveGrupo(grupo),
                        Fase = NomeFase(j.Fase),
                        OrdemFase = OrdemFase(j.Fase),
                        ResultadoOficial = resultadoOficial,
                        EhFaseDeGrupos = ehFaseDeGrupos,
                        PlacarCasa = placarCasa,
                        PlacarVisitante = placarVisitante
                    };
                })
                .OrderBy(j => campeonato.UsaClassificacaoPorGrupos ? j.EhFaseDeGrupos ? 0 : 1 : 0)
                .ThenBy(j => campeonato.UsaClassificacaoPorGrupos && j.EhFaseDeGrupos ? GrupoOrdenacao(j.GrupoChave) : string.Empty)
                .ThenBy(j => campeonato.UsaClassificacaoPorGrupos ? j.OrdemFase : 0)
                .ThenBy(j => campeonato.UsaClassificacaoPorGrupos ? j.Fase : string.Empty)
                .ThenBy(j => campeonato.UsaClassificacaoPorGrupos ? 0 : j.Rodada ?? int.MaxValue)
                .ThenBy(j => j.DataJogo)
                .ToList();

            var placaresEfetivos = jogosViewModel
                .Where(j => j.TemPlacar)
                .ToDictionary(j => j.JogoId, j => (Casa: j.PlacarCasa, Visitante: j.PlacarVisitante));

            var classificacao = simulado
                ? CalcularClassificacao(campeonato, jogos, campeonatoTimes, gruposPorTime, placaresEfetivos, ObterGrupoJogo)
                : new List<SimuladorClassificacaoViewModel>();

            return new SimuladorViewModel
            {
                Campeonato = campeonato,
                Jogos = jogosViewModel,
                Classificacao = classificacao,
                Simulado = simulado
            };
        }

        private static List<SimuladorClassificacaoViewModel> CalcularClassificacao(
            Campeonato campeonato,
            List<Jogo> jogos,
            List<CampeonatoTime> campeonatoTimes,
            Dictionary<int, string> gruposPorTime,
            Dictionary<int, (int? Casa, int? Visitante)> placares,
            Func<Jogo, string?> obterGrupoJogo)
        {
            var linhas = new Dictionary<int, SimuladorClassificacaoViewModel>();

            foreach (var campeonatoTime in campeonatoTimes.Where(ct => ct.Time != null))
            {
                var grupo = campeonatoTime.Grupo?.Nome;

                GarantirLinha(linhas, campeonatoTime.TimeId, campeonatoTime.Time, grupo, campeonato.UsaClassificacaoPorGrupos);
            }

            foreach (var jogo in jogos)
            {
                var grupo = obterGrupoJogo(jogo);

                if (campeonato.UsaClassificacaoPorGrupos && !EhJogoFaseGrupos(jogo, grupo))
                {
                    continue;
                }

                GarantirLinha(linhas, jogo.TimeCasaId, jogo.TimeCasa, grupo, campeonato.UsaClassificacaoPorGrupos);
                GarantirLinha(linhas, jogo.TimeVisitanteId, jogo.TimeVisitante, grupo, campeonato.UsaClassificacaoPorGrupos);

                if (!placares.TryGetValue(jogo.Id, out var placar) ||
                    !placar.Casa.HasValue ||
                    !placar.Visitante.HasValue)
                {
                    continue;
                }

                var linhaCasa = linhas[jogo.TimeCasaId];
                var linhaVisitante = linhas[jogo.TimeVisitanteId];

                AplicarResultado(linhaCasa, linhaVisitante, placar.Casa.Value, placar.Visitante.Value);
            }

            foreach (var grupoTime in gruposPorTime)
            {
                if (linhas.TryGetValue(grupoTime.Key, out var linha) &&
                    string.IsNullOrWhiteSpace(linha.GrupoChave))
                {
                    linha.Grupo = NomeGrupo(grupoTime.Value);
                    linha.GrupoChave = ChaveGrupo(grupoTime.Value);
                }
            }

            var ordenada = linhas.Values
                .OrderBy(l => campeonato.UsaClassificacaoPorGrupos ? GrupoOrdenacao(l.GrupoChave) : string.Empty)
                .ThenByDescending(l => l.Pontos)
                .ThenByDescending(l => l.Vitorias)
                .ThenByDescending(l => l.SaldoGols)
                .ThenByDescending(l => l.GolsPro)
                .ThenBy(l => l.TimeNome)
                .ToList();

            if (campeonato.UsaClassificacaoPorGrupos)
            {
                foreach (var grupo in ordenada.GroupBy(l => l.GrupoChave))
                {
                    var posicao = 1;

                    foreach (var linha in grupo)
                    {
                        linha.Posicao = posicao++;
                    }
                }
            }
            else
            {
                for (var i = 0; i < ordenada.Count; i++)
                {
                    ordenada[i].Posicao = i + 1;
                }
            }

            return ordenada;
        }

        private static void GarantirLinha(
            Dictionary<int, SimuladorClassificacaoViewModel> linhas,
            int timeId,
            Time? time,
            string? grupo,
            bool usaGrupos)
        {
            if (!linhas.TryGetValue(timeId, out var linha))
            {
                linha = new SimuladorClassificacaoViewModel
                {
                    TimeId = timeId,
                    TimeNome = time?.Nome ?? $"Time {timeId}",
                    EscudoUrl = time?.EscudoUrl,
                    Sigla = ObterSigla(time, "TIM"),
                    Grupo = usaGrupos ? NomeGrupo(grupo) : "Geral",
                    GrupoChave = usaGrupos ? ChaveGrupo(grupo) : string.Empty
                };

                linhas[timeId] = linha;
                return;
            }

            if (usaGrupos && string.IsNullOrWhiteSpace(linha.GrupoChave) && !string.IsNullOrWhiteSpace(grupo))
            {
                linha.Grupo = NomeGrupo(grupo);
                linha.GrupoChave = ChaveGrupo(grupo);
            }
        }

        private static void AplicarResultado(
            SimuladorClassificacaoViewModel casa,
            SimuladorClassificacaoViewModel visitante,
            int golsCasa,
            int golsVisitante)
        {
            casa.Jogos++;
            visitante.Jogos++;

            casa.GolsPro += golsCasa;
            casa.GolsContra += golsVisitante;
            visitante.GolsPro += golsVisitante;
            visitante.GolsContra += golsCasa;

            if (golsCasa > golsVisitante)
            {
                casa.Vitorias++;
                casa.Pontos += 3;
                visitante.Derrotas++;
            }
            else if (golsCasa < golsVisitante)
            {
                visitante.Vitorias++;
                visitante.Pontos += 3;
                casa.Derrotas++;
            }
            else
            {
                casa.Empates++;
                visitante.Empates++;
                casa.Pontos++;
                visitante.Pontos++;
            }
        }

        private static bool EhResultadoOficial(Jogo jogo)
        {
            return string.Equals(jogo.Status, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                   jogo.GolsCasa.HasValue &&
                   jogo.GolsVisitante.HasValue;
        }

        private static bool EhJogoFaseGrupos(Jogo jogo, string? grupo)
        {
            if (EhFaseEliminatoria(jogo.Fase))
            {
                return false;
            }

            return EhFaseDeGrupos(jogo.Fase) || !string.IsNullOrWhiteSpace(grupo);
        }

        private static bool EhFaseDeGrupos(string? fase)
        {
            return !string.IsNullOrWhiteSpace(fase) &&
                   (fase.Contains("grupo", StringComparison.OrdinalIgnoreCase) ||
                    fase.Contains("group", StringComparison.OrdinalIgnoreCase));
        }

        private static bool EhFaseEliminatoria(string? fase)
        {
            if (string.IsNullOrWhiteSpace(fase))
            {
                return false;
            }

            var texto = fase.Trim();

            return texto.Contains("32", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("16avos", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("16 avos", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("dezesseisavos", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("round of 32", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("round of 16", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("oitava", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("oitavo", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("quarter", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("quarta", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("semi", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("final", StringComparison.OrdinalIgnoreCase);
        }

        private static int? NormalizarPlacar(int? placar)
        {
            return placar is >= 0 and <= 99
                ? placar
                : null;
        }

        private static string NomeGrupo(string? grupo)
        {
            if (string.IsNullOrWhiteSpace(grupo))
            {
                return "Sem grupo";
            }

            var texto = grupo.Trim();

            return texto.StartsWith("Grupo ", StringComparison.OrdinalIgnoreCase)
                ? texto
                : $"Grupo {texto}";
        }

        private static string ChaveGrupo(string? grupo)
        {
            if (string.IsNullOrWhiteSpace(grupo))
            {
                return string.Empty;
            }

            var texto = grupo.Trim();

            return texto.StartsWith("Grupo ", StringComparison.OrdinalIgnoreCase)
                ? texto.Substring("Grupo ".Length).Trim()
                : texto;
        }

        private static string GrupoOrdenacao(string? grupo)
        {
            return string.IsNullOrWhiteSpace(grupo)
                ? "ZZZ"
                : grupo;
        }

        private static string NomeFase(string? fase)
        {
            if (string.IsNullOrWhiteSpace(fase))
            {
                return "Fase a definir";
            }

            var texto = fase.Trim();

            if (EhFaseDeGrupos(texto))
            {
                return "Fase de grupos";
            }

            if (texto.Contains("32", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16avos", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16 avos", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("dezesseisavos", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("round of 32", StringComparison.OrdinalIgnoreCase))
            {
                return "16Avos-de-final";
            }

            if (texto.Contains("round of 16", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("oitava", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("oitavo", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16", StringComparison.OrdinalIgnoreCase))
            {
                return "Oitavas";
            }

            if (texto.Contains("quarter", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("quarta", StringComparison.OrdinalIgnoreCase))
            {
                return "Quartas";
            }

            if (texto.Contains("semi", StringComparison.OrdinalIgnoreCase))
            {
                return "Semifinais";
            }

            if (texto.Contains("final", StringComparison.OrdinalIgnoreCase))
            {
                return "Final";
            }

            return texto;
        }

        private static int OrdemFase(string? fase)
        {
            var texto = fase ?? string.Empty;

            if (EhFaseDeGrupos(texto))
            {
                return 1;
            }

            if (texto.Contains("32", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16avos", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16 avos", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("dezesseisavos", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("round of 32", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (texto.Contains("round of 16", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("oitava", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("oitavo", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("16", StringComparison.OrdinalIgnoreCase))
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

        private static string ObterSigla(Time? time, string fallback)
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
    }
}
