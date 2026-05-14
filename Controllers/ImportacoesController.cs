using FutPlay.Data;
using FutPlay.Models;
using FutPlay.Services;
using FutPlay.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FutPlay.Controllers
{
    [Authorize]
    public class ImportacoesController : Controller
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;

        public ImportacoesController(
            FootballApiService footballApiService,
            AppDbContext context)
        {
            _footballApiService = footballApiService;
            _context = context;
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
                var resultado = await _footballApiService.BuscarLigasAsync(pais, temporada);

                if (resultado.RootElement.TryGetProperty("response", out var response))
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var league = item.GetProperty("league");
                        var country = item.GetProperty("country");

                        int apiLeagueId = league.GetProperty("id").GetInt32();
                        string nome = league.GetProperty("name").GetString() ?? "";
                        string tipo = league.GetProperty("type").GetString() ?? "";
                        string paisNome = country.GetProperty("name").GetString() ?? "";
                        string? logo = league.TryGetProperty("logo", out var logoElement)
                            ? logoElement.GetString()
                            : null;

                        bool jaImportado = await _context.Campeonatos
                            .AnyAsync(c => c.ApiLeagueId == apiLeagueId && c.Ano == temporada);

                        ligas.Add(new ApiLigaViewModel
                        {
                            ApiLeagueId = apiLeagueId,
                            Nome = nome,
                            Tipo = tipo,
                            Pais = paisNome,
                            Temporada = temporada,
                            LogoUrl = logo,
                            JaImportado = jaImportado
                        });
                    }
                }

                ViewBag.Pais = pais;
                ViewBag.Temporada = temporada;
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
            int temporada)
        {
            var jaExiste = await _context.Campeonatos
                .AnyAsync(c => c.ApiLeagueId == apiLeagueId && c.Ano == temporada);

            if (jaExiste)
            {
                TempData["Erro"] = "Este campeonato já foi importado.";
                return RedirectToAction(nameof(Index));
            }

            var campeonato = new Campeonato
            {
                Nome = nome,
                Ano = temporada,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "Liga" : tipo,
                Ativo = true,
                ApiLeagueId = apiLeagueId
            };

            _context.Campeonatos.Add(campeonato);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Campeonato {nome} importado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarJogos(int campeonatoId)
        {
            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                TempData["Erro"] = "Campeonato não encontrado.";
                return RedirectToAction("Index", "Campeonatos");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                TempData["Erro"] = "Este campeonato não possui ID da API.";
                return RedirectToAction("Index", "Campeonatos");
            }

            try
            {
                var resultado = await _footballApiService.BuscarJogosAsync(
                    campeonato.ApiLeagueId.Value,
                    campeonato.Ano
                );

                int jogosImportados = 0;
                int timesImportados = 0;

                if (resultado.RootElement.TryGetProperty("response", out var response))
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var fixture = item.GetProperty("fixture");
                        var teams = item.GetProperty("teams");
                        var goals = item.GetProperty("goals");
                        var league = item.GetProperty("league");

                        int apiFixtureId = fixture.GetProperty("id").GetInt32();

                        bool jogoJaExiste = await _context.Jogos
                            .AnyAsync(j => j.ApiFixtureId == apiFixtureId);

                        if (jogoJaExiste)
                        {
                            continue;
                        }

                        var teamHome = teams.GetProperty("home");
                        var teamAway = teams.GetProperty("away");

                        int apiTimeCasaId = teamHome.GetProperty("id").GetInt32();
                        int apiTimeVisitanteId = teamAway.GetProperty("id").GetInt32();

                        string nomeTimeCasa = teamHome.GetProperty("name").GetString() ?? "";
                        string nomeTimeVisitante = teamAway.GetProperty("name").GetString() ?? "";

                        string? logoCasa = teamHome.TryGetProperty("logo", out var logoCasaElement)
                            ? logoCasaElement.GetString()
                            : null;

                        string? logoVisitante = teamAway.TryGetProperty("logo", out var logoVisitanteElement)
                            ? logoVisitanteElement.GetString()
                            : null;

                        var timeCasa = await ObterOuCriarTimeApi(
                            apiTimeCasaId,
                            nomeTimeCasa,
                            logoCasa
                        );

                        if (timeCasa.Criado)
                        {
                            timesImportados++;
                        }

                        var timeVisitante = await ObterOuCriarTimeApi(
                            apiTimeVisitanteId,
                            nomeTimeVisitante,
                            logoVisitante
                        );

                        if (timeVisitante.Criado)
                        {
                            timesImportados++;
                        }

                        DateTime dataJogo = fixture.GetProperty("date").GetDateTime();

                        string statusApi = fixture
                            .GetProperty("status")
                            .GetProperty("short")
                            .GetString() ?? "";

                        string status = ConverterStatusJogo(statusApi);

                        int? golsCasa = null;
                        int? golsVisitante = null;

                        if (goals.TryGetProperty("home", out var golsCasaElement) &&
                            golsCasaElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            golsCasa = golsCasaElement.GetInt32();
                        }

                        if (goals.TryGetProperty("away", out var golsVisitanteElement) &&
                            golsVisitanteElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            golsVisitante = golsVisitanteElement.GetInt32();
                        }

                        string? rodada = league.TryGetProperty("round", out var roundElement)
                            ? roundElement.GetString()
                            : null;

                        var jogo = new Jogo
                        {
                            CampeonatoId = campeonato.Id,
                            TimeCasaId = timeCasa.Time.Id,
                            TimeVisitanteId = timeVisitante.Time.Id,
                            DataJogo = dataJogo,
                            Fase = rodada,
                            Grupo = null,
                            Rodada = null,
                            GolsCasa = golsCasa,
                            GolsVisitante = golsVisitante,
                            Status = status,
                            Ativo = true,
                            ApiFixtureId = apiFixtureId
                        };

                        _context.Jogos.Add(jogo);
                        jogosImportados++;
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Sucesso"] = $"Importação concluída. Jogos importados: {jogosImportados}. Times importados: {timesImportados}.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao importar jogos: {ex.Message}";
            }

            return RedirectToAction("Index", "Campeonatos");
        }

        private async Task<(Time Time, bool Criado)> ObterOuCriarTimeApi(
            int apiTeamId,
            string nome,
            string? logoUrl)
        {
            var time = await _context.Times
                .FirstOrDefaultAsync(t => t.ApiTeamId == apiTeamId);

            if (time != null)
            {
                return (time, false);
            }

            time = new Time
            {
                Nome = nome,
                Sigla = GerarSigla(nome),
                Pais = null,
                Tipo = "Clube",
                EscudoUrl = logoUrl,
                Ativo = true,
                ApiTeamId = apiTeamId
            };

            _context.Times.Add(time);

            await _context.SaveChangesAsync();

            return (time, true);
        }

        private string GerarSigla(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "";
            }

            var partes = nome
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(3)
                .ToList();

            if (partes.Count == 1)
            {
                return partes[0].Length >= 3
                    ? partes[0].Substring(0, 3).ToUpper()
                    : partes[0].ToUpper();
            }

            return string.Join("", partes.Select(p => p[0])).ToUpper();
        }

        private string ConverterStatusJogo(string statusApi)
        {
            return statusApi switch
            {
                "NS" => "Agendado",
                "TBD" => "Agendado",
                "1H" => "Em andamento",
                "HT" => "Em andamento",
                "2H" => "Em andamento",
                "ET" => "Em andamento",
                "BT" => "Em andamento",
                "P" => "Em andamento",
                "SUSP" => "Cancelado",
                "INT" => "Cancelado",
                "FT" => "Finalizado",
                "AET" => "Finalizado",
                "PEN" => "Finalizado",
                "PST" => "Cancelado",
                "CANC" => "Cancelado",
                "ABD" => "Cancelado",
                "AWD" => "Finalizado",
                "WO" => "Finalizado",
                _ => "Agendado"
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarResultados(int campeonatoId)
        {
            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                TempData["Erro"] = "Campeonato não encontrado.";
                return RedirectToAction("Index", "Campeonatos");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                TempData["Erro"] = "Este campeonato não possui ID da API.";
                return RedirectToAction("Index", "Campeonatos");
            }

            try
            {
                var resultado = await _footballApiService.BuscarJogosAsync(
                    campeonato.ApiLeagueId.Value,
                    campeonato.Ano
                );

                int jogosAtualizados = 0;

                if (resultado.RootElement.TryGetProperty("response", out var response))
                {
                    foreach (var item in response.EnumerateArray())
                    {
                        var fixture = item.GetProperty("fixture");
                        var goals = item.GetProperty("goals");

                        int apiFixtureId = fixture.GetProperty("id").GetInt32();

                        var jogo = await _context.Jogos
                            .FirstOrDefaultAsync(j => j.ApiFixtureId == apiFixtureId);

                        if (jogo == null)
                        {
                            continue;
                        }

                        DateTime dataJogo = fixture.GetProperty("date").GetDateTime();

                        string statusApi = fixture
                            .GetProperty("status")
                            .GetProperty("short")
                            .GetString() ?? "";

                        string status = ConverterStatusJogo(statusApi);

                        int? golsCasa = null;
                        int? golsVisitante = null;

                        if (goals.TryGetProperty("home", out var golsCasaElement) &&
                            golsCasaElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            golsCasa = golsCasaElement.GetInt32();
                        }

                        if (goals.TryGetProperty("away", out var golsVisitanteElement) &&
                            golsVisitanteElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            golsVisitante = golsVisitanteElement.GetInt32();
                        }

                        jogo.DataJogo = dataJogo;
                        jogo.Status = status;
                        jogo.GolsCasa = golsCasa;
                        jogo.GolsVisitante = golsVisitante;

                        _context.Jogos.Update(jogo);
                        jogosAtualizados++;
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Sucesso"] = $"Resultados atualizados com sucesso. Jogos atualizados: {jogosAtualizados}.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar resultados: {ex.Message}";
            }

            return RedirectToAction("Index", "Campeonatos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SincronizarCampeonato(int campeonatoId)
        {
            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                TempData["Erro"] = "Campeonato não encontrado.";
                return RedirectToAction("Index", "Campeonatos");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                TempData["Erro"] = "Este campeonato não possui ID da API.";
                return RedirectToAction("Index", "Campeonatos");
            }

            try
            {
                int jogosAtualizados = await AtualizarResultadosInterno(campeonato);
                await RecalcularClassificacaoInterno(campeonato.Id);
                await RecalcularPontuacaoPalpitesInterno(campeonato.Id);

                TempData["Sucesso"] =
                    $"Sincronização concluída. Jogos atualizados: {jogosAtualizados}. Classificação e palpites recalculados.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao sincronizar campeonato: {ex.Message}";
            }

            return RedirectToAction("Portal", "Campeonatos", new { id = campeonato.Id });
        }

        private async Task<int> AtualizarResultadosInterno(Campeonato campeonato)
        {
            var resultado = await _footballApiService.BuscarJogosAsync(
                campeonato.ApiLeagueId!.Value,
                campeonato.Ano
            );

            int jogosAtualizados = 0;

            if (resultado.RootElement.TryGetProperty("response", out var response))
            {
                foreach (var item in response.EnumerateArray())
                {
                    var fixture = item.GetProperty("fixture");
                    var goals = item.GetProperty("goals");

                    int apiFixtureId = fixture.GetProperty("id").GetInt32();

                    var jogo = await _context.Jogos
                        .FirstOrDefaultAsync(j => j.ApiFixtureId == apiFixtureId);

                    if (jogo == null)
                    {
                        continue;
                    }

                    DateTime dataJogo = fixture.GetProperty("date").GetDateTime();

                    string statusApi = fixture
                        .GetProperty("status")
                        .GetProperty("short")
                        .GetString() ?? "";

                    string status = ConverterStatusJogo(statusApi);

                    int? golsCasa = null;
                    int? golsVisitante = null;

                    if (goals.TryGetProperty("home", out var golsCasaElement) &&
                        golsCasaElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        golsCasa = golsCasaElement.GetInt32();
                    }

                    if (goals.TryGetProperty("away", out var golsVisitanteElement) &&
                        golsVisitanteElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        golsVisitante = golsVisitanteElement.GetInt32();
                    }

                    jogo.DataJogo = dataJogo;
                    jogo.Status = status;
                    jogo.GolsCasa = golsCasa;
                    jogo.GolsVisitante = golsVisitante;

                    _context.Jogos.Update(jogo);
                    jogosAtualizados++;
                }

                await _context.SaveChangesAsync();
            }

            return jogosAtualizados;
        }

        private async Task RecalcularClassificacaoInterno(int campeonatoId)
        {
            var jogosFinalizados = await _context.Jogos
                .Where(j =>
                    j.CampeonatoId == campeonatoId &&
                    j.Ativo &&
                    j.Status == "Finalizado" &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue)
                .ToListAsync();

            var classificacoesAtuais = await _context.Classificacoes
                .Where(c => c.CampeonatoId == campeonatoId)
                .ToListAsync();

            _context.Classificacoes.RemoveRange(classificacoesAtuais);

            var tabela = new Dictionary<int, Classificacao>();

            foreach (var jogo in jogosFinalizados)
            {
                if (!tabela.ContainsKey(jogo.TimeCasaId))
                {
                    tabela[jogo.TimeCasaId] = CriarClassificacaoInicial(campeonatoId, jogo.TimeCasaId, jogo.Grupo);
                }

                if (!tabela.ContainsKey(jogo.TimeVisitanteId))
                {
                    tabela[jogo.TimeVisitanteId] = CriarClassificacaoInicial(campeonatoId, jogo.TimeVisitanteId, jogo.Grupo);
                }

                var casa = tabela[jogo.TimeCasaId];
                var visitante = tabela[jogo.TimeVisitanteId];

                int golsCasa = jogo.GolsCasa.Value;
                int golsVisitante = jogo.GolsVisitante.Value;

                casa.Jogos++;
                visitante.Jogos++;

                casa.GolsPro += golsCasa;
                casa.GolsContra += golsVisitante;

                visitante.GolsPro += golsVisitante;
                visitante.GolsContra += golsCasa;

                casa.SaldoGols = casa.GolsPro - casa.GolsContra;
                visitante.SaldoGols = visitante.GolsPro - visitante.GolsContra;

                if (golsCasa > golsVisitante)
                {
                    casa.Vitorias++;
                    casa.Pontos += 3;
                    visitante.Derrotas++;
                }
                else if (golsVisitante > golsCasa)
                {
                    visitante.Vitorias++;
                    visitante.Pontos += 3;
                    casa.Derrotas++;
                }
                else
                {
                    casa.Empates++;
                    visitante.Empates++;
                    casa.Pontos += 1;
                    visitante.Pontos += 1;
                }
            }

            var classificacoesOrdenadas = tabela.Values
                .OrderBy(c => c.Grupo)
                .ThenByDescending(c => c.Pontos)
                .ThenByDescending(c => c.Vitorias)
                .ThenByDescending(c => c.SaldoGols)
                .ThenByDescending(c => c.GolsPro)
                .ToList();

            var grupos = classificacoesOrdenadas
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Grupo) ? "" : c.Grupo);

            foreach (var grupo in grupos)
            {
                int posicao = 1;

                foreach (var item in grupo)
                {
                    item.Posicao = posicao;
                    posicao++;
                }
            }

            _context.Classificacoes.AddRange(classificacoesOrdenadas);

            await _context.SaveChangesAsync();
        }

        private Classificacao CriarClassificacaoInicial(int campeonatoId, int timeId, string? grupo)
        {
            return new Classificacao
            {
                CampeonatoId = campeonatoId,
                TimeId = timeId,
                Grupo = grupo,
                Posicao = 0,
                Pontos = 0,
                Jogos = 0,
                Vitorias = 0,
                Empates = 0,
                Derrotas = 0,
                GolsPro = 0,
                GolsContra = 0,
                SaldoGols = 0,
                Ativo = true
            };
        }

        private async Task RecalcularPontuacaoPalpitesInterno(int campeonatoId)
        {
            var palpites = await _context.Palpites
                .Include(p => p.Jogo)
                .Where(p =>
                    p.Ativo &&
                    p.Jogo != null &&
                    p.Jogo.CampeonatoId == campeonatoId)
                .ToListAsync();

            foreach (var palpite in palpites)
            {
                if (palpite.Jogo != null &&
                    palpite.Jogo.Status == "Finalizado" &&
                    palpite.Jogo.GolsCasa.HasValue &&
                    palpite.Jogo.GolsVisitante.HasValue)
                {
                    palpite.PontosGanhos = CalcularPontuacao(palpite, palpite.Jogo);
                }
                else
                {
                    palpite.PontosGanhos = 0;
                }
            }

            await _context.SaveChangesAsync();

            var participantesAfetados = palpites
                .Select(p => p.LigaParticipanteId)
                .Distinct()
                .ToList();

            foreach (var participanteId in participantesAfetados)
            {
                var participante = await _context.LigaParticipantes
                    .FirstOrDefaultAsync(p => p.Id == participanteId);

                if (participante == null)
                {
                    continue;
                }

                participante.PontuacaoTotal = await _context.Palpites
                    .Where(p =>
                        p.LigaParticipanteId == participante.Id &&
                        p.LigaId == participante.LigaId &&
                        p.Ativo)
                    .SumAsync(p => p.PontosGanhos);

                _context.LigaParticipantes.Update(participante);
            }

            await _context.SaveChangesAsync();
        }

        private int CalcularPontuacao(Palpite palpite, Jogo jogo)
        {
            if (!jogo.GolsCasa.HasValue || !jogo.GolsVisitante.HasValue)
            {
                return 0;
            }

            int golsCasaReal = jogo.GolsCasa.Value;
            int golsVisitanteReal = jogo.GolsVisitante.Value;

            int golsCasaPalpite = palpite.GolsCasaPalpite;
            int golsVisitantePalpite = palpite.GolsVisitantePalpite;

            if (golsCasaReal == golsCasaPalpite &&
                golsVisitanteReal == golsVisitantePalpite)
            {
                return 10;
            }

            int pontos = 0;

            string resultadoReal = ObterResultado(golsCasaReal, golsVisitanteReal);
            string resultadoPalpite = ObterResultado(golsCasaPalpite, golsVisitantePalpite);

            if (resultadoReal == resultadoPalpite)
            {
                pontos += 5;
            }

            if (golsCasaReal == golsCasaPalpite)
            {
                pontos += 2;
            }

            if (golsVisitanteReal == golsVisitantePalpite)
            {
                pontos += 2;
            }

            return pontos;
        }

        private string ObterResultado(int golsCasa, int golsVisitante)
        {
            if (golsCasa > golsVisitante)
            {
                return "Casa";
            }

            if (golsVisitante > golsCasa)
            {
                return "Visitante";
            }

            return "Empate";
        }

    }
}