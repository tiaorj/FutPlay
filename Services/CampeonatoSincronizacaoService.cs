using FutPlay.Data;
using FutPlay.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FutPlay.Services
{
    public class CampeonatoSincronizacaoService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;
        private readonly ImportacaoTimesService _importacaoTimesService;
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;
        private readonly ApiSyncLogService _apiSyncLogService;
        private readonly ILogger<CampeonatoSincronizacaoService> _logger;

        public CampeonatoSincronizacaoService(
            FootballApiService footballApiService,
            AppDbContext context,
            ImportacaoTimesService importacaoTimesService,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService,
            ApiSyncLogService apiSyncLogService,
            ILogger<CampeonatoSincronizacaoService> logger)
        {
            _footballApiService = footballApiService;
            _context = context;
            _importacaoTimesService = importacaoTimesService;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
            _apiSyncLogService = apiSyncLogService;
            _logger = logger;
        }

        public Task<CampeonatoSincronizacaoResultado> SincronizarCampeonatoAsync(
            int campeonatoId,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            return SincronizarJogosCompeticaoAsync(campeonatoId, usuarioId, usuarioEmail);
        }

        public async Task<CampeonatoSincronizacaoResultado> SincronizarJogosCompeticaoAsync(
            int campeonatoId,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            var inicio = DateTime.UtcNow;
            _logger.LogInformation("Iniciando sincronizacao de jogos. CampeonatoId: {CampeonatoId}", campeonatoId);

            var campeonato = await ObterCampeonatoApiAsync(campeonatoId);

            if (campeonato == null)
            {
                return CampeonatoSincronizacaoResultado.Falha("Campeonato não encontrado.");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                var falha = CampeonatoSincronizacaoResultado.Falha(
                    "Este campeonato não possui ID da API.",
                    campeonato.Id,
                    redirecionarParaPortal: true);

                await RegistrarLogAsync("JogosCompeticao", campeonato, inicio, falha, usuarioId, usuarioEmail);

                return falha;
            }

            var resultado = CampeonatoSincronizacaoResultado.Iniciar(campeonato.Id, redirecionarParaPortal: true);

            try
            {
                using var respostaApi = await _footballApiService.BuscarJogosAsync(campeonato.ApiLeagueId.Value, campeonato.Ano);
                var fixtures = ExtrairFixtures(respostaApi).ToList();

                resultado.TotalProcessados = fixtures.Count;

                var jogosCampeonato = await _context.Jogos
                    .Where(j => j.CampeonatoId == campeonato.Id)
                    .ToListAsync();

                var jogosPorFixture = jogosCampeonato
                    .Where(j => j.ApiFixtureId.HasValue)
                    .GroupBy(j => j.ApiFixtureId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var houveJogoFinalizadoAlterado = false;

                foreach (var fixture in fixtures)
                {
                    try
                    {
                        var timeCasa = await SincronizarTimeAsync(fixture.TimeCasa, campeonato, resultado);
                        var timeVisitante = await SincronizarTimeAsync(fixture.TimeVisitante, campeonato, resultado);

                        if (timeCasa == null || timeVisitante == null)
                        {
                            resultado.JogosIgnorados++;
                            continue;
                        }

                        var jogo = jogosPorFixture.TryGetValue(fixture.ApiFixtureId, out var jogoPorFixture)
                            ? jogoPorFixture
                            : LocalizarJogoSemFixture(jogosCampeonato, fixture, timeCasa.Id, timeVisitante.Id);

                        if (jogo == null)
                        {
                            jogo = new Jogo
                            {
                                CampeonatoId = campeonato.Id,
                                TimeCasaId = timeCasa.Id,
                                TimeVisitanteId = timeVisitante.Id,
                                DataJogo = fixture.DataJogo,
                                Rodada = fixture.Rodada,
                                Fase = fixture.Fase,
                                Grupo = fixture.Grupo,
                                Status = fixture.Status,
                                GolsCasa = fixture.GolsCasa,
                                GolsVisitante = fixture.GolsVisitante,
                                ApiFixtureId = fixture.ApiFixtureId,
                                Ativo = true
                            };

                            _context.Jogos.Add(jogo);
                            jogosCampeonato.Add(jogo);
                            jogosPorFixture[fixture.ApiFixtureId] = jogo;
                            resultado.JogosCriados++;

                            if (fixture.EstaFinalizado)
                            {
                                resultado.JogosFinalizados++;
                                houveJogoFinalizadoAlterado = true;
                            }

                            continue;
                        }

                        if (jogo.CampeonatoId != campeonato.Id)
                        {
                            resultado.JogosIgnorados++;
                            continue;
                        }

                        var alterado = AplicarDadosJogo(jogo, fixture, timeCasa.Id, timeVisitante.Id, limparPlacarSeNaoIniciado: true);

                        if (alterado)
                        {
                            _context.Jogos.Update(jogo);
                            resultado.JogosAtualizados++;

                            if (fixture.EstaFinalizado)
                            {
                                resultado.JogosFinalizados++;
                                houveJogoFinalizadoAlterado = true;
                            }
                        }
                        else
                        {
                            resultado.JogosIgnorados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.Erros.Add($"Fixture {fixture.ApiFixtureId}: {ex.Message}");
                        _logger.LogError(ex, "Erro ao sincronizar fixture. ApiFixtureId: {ApiFixtureId}", fixture.ApiFixtureId);
                    }
                }

                await _context.SaveChangesAsync();

                if (houveJogoFinalizadoAlterado)
                {
                    await RecalcularClassificacaoAsync(campeonato, resultado, usuarioId, usuarioEmail);
                }

                resultado.Sucesso = !resultado.Erros.Any();
                resultado.Mensagem = MontarMensagemSincronizacao(resultado, "Sincronização de jogos concluída");

                await RegistrarLogAsync("JogosCompeticao", campeonato, inicio, resultado, usuarioId, usuarioEmail);

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar jogos. CampeonatoId: {CampeonatoId}", campeonato.Id);

                resultado.Sucesso = false;
                resultado.Mensagem = "Erro ao sincronizar jogos da competição.";
                resultado.Erros.Add(ex.Message);

                await RegistrarLogAsync("JogosCompeticao", campeonato, inicio, resultado, usuarioId, usuarioEmail, ex.ToString());

                return resultado;
            }
        }

        public async Task<CampeonatoSincronizacaoResultado> AtualizarResultadosAsync(
            int campeonatoId,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            var inicio = DateTime.UtcNow;
            _logger.LogInformation("Iniciando atualizacao de resultados. CampeonatoId: {CampeonatoId}", campeonatoId);

            var campeonato = await ObterCampeonatoApiAsync(campeonatoId);

            if (campeonato == null)
            {
                return CampeonatoSincronizacaoResultado.Falha("Campeonato não encontrado.");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                var falha = CampeonatoSincronizacaoResultado.Falha(
                    "Este campeonato não possui ID da API.",
                    campeonato.Id,
                    redirecionarParaPortal: true);

                await RegistrarLogAsync("Resultados", campeonato, inicio, falha, usuarioId, usuarioEmail);

                return falha;
            }

            var resultado = CampeonatoSincronizacaoResultado.Iniciar(campeonato.Id, redirecionarParaPortal: true);

            try
            {
                using var respostaApi = await _footballApiService.BuscarJogosAsync(campeonato.ApiLeagueId.Value, campeonato.Ano);
                var fixtures = ExtrairFixtures(respostaApi).ToList();

                resultado.TotalProcessados = fixtures.Count;

                var jogosComFixture = await _context.Jogos
                    .Where(j => j.CampeonatoId == campeonato.Id && j.ApiFixtureId.HasValue)
                    .ToListAsync();

                var jogosPorFixture = jogosComFixture
                    .GroupBy(j => j.ApiFixtureId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var fixture in fixtures)
                {
                    try
                    {
                        if (!jogosPorFixture.TryGetValue(fixture.ApiFixtureId, out var jogo))
                        {
                            resultado.JogosIgnorados++;
                            continue;
                        }

                        var alterado = AplicarDadosJogo(
                            jogo,
                            fixture,
                            timeCasaId: null,
                            timeVisitanteId: null,
                            limparPlacarSeNaoIniciado: true);

                        if (!alterado)
                        {
                            resultado.JogosIgnorados++;
                            continue;
                        }

                        _context.Jogos.Update(jogo);
                        resultado.JogosAtualizados++;

                        if (fixture.EstaFinalizado)
                        {
                            resultado.JogosFinalizados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.Erros.Add($"Fixture {fixture.ApiFixtureId}: {ex.Message}");
                        _logger.LogError(ex, "Erro ao atualizar resultado de fixture. ApiFixtureId: {ApiFixtureId}", fixture.ApiFixtureId);
                    }
                }

                await _context.SaveChangesAsync();

                if (resultado.JogosAtualizados > 0)
                {
                    await RecalcularClassificacaoAsync(campeonato, resultado, usuarioId, usuarioEmail);
                }

                resultado.Sucesso = !resultado.Erros.Any();
                resultado.Mensagem = MontarMensagemSincronizacao(resultado, "Atualização de resultados concluída");

                await RegistrarLogAsync("Resultados", campeonato, inicio, resultado, usuarioId, usuarioEmail);

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar resultados. CampeonatoId: {CampeonatoId}", campeonato.Id);

                resultado.Sucesso = false;
                resultado.Mensagem = "Erro ao atualizar resultados da competição.";
                resultado.Erros.Add(ex.Message);

                await RegistrarLogAsync("Resultados", campeonato, inicio, resultado, usuarioId, usuarioEmail, ex.ToString());

                return resultado;
            }
        }

        private async Task<Campeonato?> ObterCampeonatoApiAsync(int campeonatoId)
        {
            return await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);
        }

        private async Task<Time?> SincronizarTimeAsync(
            ApiFixtureTime timeApi,
            Campeonato campeonato,
            CampeonatoSincronizacaoResultado resultado)
        {
            var sincronizacao = await _importacaoTimesService.SincronizarTimeApiAsync(
                timeApi.ApiTeamId,
                timeApi.Nome,
                campeonato.Pais,
                timeApi.EscudoUrl,
                sigla: null,
                timeApi.Tipo);

            if (!sincronizacao.Sucesso || sincronizacao.Time == null)
            {
                resultado.Erros.Add(sincronizacao.Mensagem);
                return null;
            }

            if (sincronizacao.Criado)
            {
                resultado.TimesCriados++;
            }
            else if (sincronizacao.Atualizado)
            {
                resultado.TimesAtualizados++;
            }

            return sincronizacao.Time;
        }

        private async Task RecalcularClassificacaoAsync(
            Campeonato campeonato,
            CampeonatoSincronizacaoResultado resultado,
            string? usuarioId,
            string? usuarioEmail)
        {
            var inicio = DateTime.UtcNow;

            await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
            await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);

            resultado.ClassificacaoRecalculada = true;
            resultado.DataRecalculo = DateTime.Now;

            await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
            {
                TipoSincronizacao = "Classificacao",
                CampeonatoId = campeonato.Id,
                ApiLeagueId = campeonato.ApiLeagueId,
                Temporada = campeonato.Ano,
                DataInicio = inicio,
                Status = "Sucesso",
                TotalProcessados = 1,
                TotalAtualizados = 1,
                Mensagem = "Classificação recalculada automaticamente após sincronização.",
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail
            });
        }

        private async Task RegistrarLogAsync(
            string tipo,
            Campeonato campeonato,
            DateTime inicio,
            CampeonatoSincronizacaoResultado resultado,
            string? usuarioId,
            string? usuarioEmail,
            string? erroDetalhado = null)
        {
            await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
            {
                TipoSincronizacao = tipo,
                CampeonatoId = campeonato.Id,
                ApiLeagueId = campeonato.ApiLeagueId,
                Temporada = campeonato.Ano,
                DataInicio = inicio,
                Status = resultado.Sucesso
                    ? "Sucesso"
                    : resultado.Erros.Any() && (resultado.JogosCriados > 0 || resultado.JogosAtualizados > 0 || resultado.TimesCriados > 0 || resultado.TimesAtualizados > 0)
                        ? "Parcial"
                        : "Erro",
                TotalProcessados = resultado.TotalProcessados,
                TotalCriados = resultado.JogosCriados + resultado.TimesCriados,
                TotalAtualizados = resultado.JogosAtualizados + resultado.TimesAtualizados,
                TotalIgnorados = resultado.JogosIgnorados,
                Mensagem = resultado.Mensagem,
                ErroDetalhado = erroDetalhado ?? (resultado.Erros.Any() ? string.Join(Environment.NewLine, resultado.Erros) : null),
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail
            });
        }

        private static IEnumerable<ApiFixtureData> ExtrairFixtures(JsonDocument resultado)
        {
            if (!resultado.RootElement.TryGetProperty("response", out var response))
            {
                yield break;
            }

            foreach (var item in response.EnumerateArray())
            {
                yield return ExtrairFixture(item);
            }
        }

        private static ApiFixtureData ExtrairFixture(JsonElement item)
        {
            var fixture = item.GetProperty("fixture");
            var teams = item.GetProperty("teams");
            var goals = item.GetProperty("goals");
            var league = item.GetProperty("league");
            var statusApi = fixture
                .GetProperty("status")
                .GetProperty("short")
                .GetString() ?? string.Empty;
            var rodadaTexto = ObterString(league, "round");

            return new ApiFixtureData
            {
                ApiFixtureId = fixture.GetProperty("id").GetInt32(),
                DataJogo = fixture.GetProperty("date").GetDateTime(),
                Status = FootballApiStatusMapper.ConverterStatusJogo(statusApi),
                StatusApi = statusApi,
                Fase = rodadaTexto,
                Grupo = ExtrairGrupo(rodadaTexto),
                Rodada = ExtrairNumeroRodada(rodadaTexto),
                GolsCasa = ObterIntNullable(goals, "home"),
                GolsVisitante = ObterIntNullable(goals, "away"),
                TimeCasa = ExtrairTime(teams.GetProperty("home")),
                TimeVisitante = ExtrairTime(teams.GetProperty("away"))
            };
        }

        private static ApiFixtureTime ExtrairTime(JsonElement team)
        {
            var nacional = ObterBoolNullable(team, "national");

            return new ApiFixtureTime
            {
                ApiTeamId = ObterInt(team, "id"),
                Nome = ObterString(team, "name") ?? string.Empty,
                EscudoUrl = ObterString(team, "logo"),
                Tipo = nacional == true ? "Seleção" : "Clube"
            };
        }

        private static Jogo? LocalizarJogoSemFixture(
            List<Jogo> jogosCampeonato,
            ApiFixtureData fixture,
            int timeCasaId,
            int timeVisitanteId)
        {
            return jogosCampeonato.FirstOrDefault(j =>
                !j.ApiFixtureId.HasValue &&
                j.TimeCasaId == timeCasaId &&
                j.TimeVisitanteId == timeVisitanteId &&
                (j.DataJogo.Date == fixture.DataJogo.Date ||
                 (fixture.Rodada.HasValue && j.Rodada == fixture.Rodada)));
        }

        private static bool AplicarDadosJogo(
            Jogo jogo,
            ApiFixtureData fixture,
            int? timeCasaId,
            int? timeVisitanteId,
            bool limparPlacarSeNaoIniciado)
        {
            var alterado = false;

            alterado |= AtualizarValor(timeCasaId, valor => jogo.TimeCasaId != valor, valor => jogo.TimeCasaId = valor);
            alterado |= AtualizarValor(timeVisitanteId, valor => jogo.TimeVisitanteId != valor, valor => jogo.TimeVisitanteId = valor);

            if (jogo.ApiFixtureId != fixture.ApiFixtureId)
            {
                jogo.ApiFixtureId = fixture.ApiFixtureId;
                alterado = true;
            }

            if (jogo.DataJogo != fixture.DataJogo)
            {
                jogo.DataJogo = fixture.DataJogo;
                alterado = true;
            }

            if (!string.IsNullOrWhiteSpace(fixture.Status) && jogo.Status != fixture.Status)
            {
                jogo.Status = fixture.Status;
                alterado = true;
            }

            alterado |= AtualizarTexto(fixture.Fase, valor => jogo.Fase != valor, valor => jogo.Fase = valor);
            alterado |= AtualizarTexto(fixture.Grupo, valor => jogo.Grupo != valor, valor => jogo.Grupo = valor);
            alterado |= AtualizarValor(fixture.Rodada, valor => jogo.Rodada != valor, valor => jogo.Rodada = valor);

            alterado |= AtualizarPlacar(
                fixture.GolsCasa,
                limparPlacarSeNaoIniciado && fixture.NaoComecou,
                () => jogo.GolsCasa,
                valor => jogo.GolsCasa = valor);

            alterado |= AtualizarPlacar(
                fixture.GolsVisitante,
                limparPlacarSeNaoIniciado && fixture.NaoComecou,
                () => jogo.GolsVisitante,
                valor => jogo.GolsVisitante = valor);

            if (!jogo.Ativo)
            {
                jogo.Ativo = true;
                alterado = true;
            }

            return alterado;
        }

        private static bool AtualizarTexto(
            string? novoValor,
            Func<string, bool> deveAtualizar,
            Action<string> atualizar)
        {
            if (string.IsNullOrWhiteSpace(novoValor))
            {
                return false;
            }

            var valor = novoValor.Trim();

            if (!deveAtualizar(valor))
            {
                return false;
            }

            atualizar(valor);
            return true;
        }

        private static bool AtualizarValor<T>(
            T? novoValor,
            Func<T, bool> deveAtualizar,
            Action<T> atualizar) where T : struct
        {
            if (!novoValor.HasValue || !deveAtualizar(novoValor.Value))
            {
                return false;
            }

            atualizar(novoValor.Value);
            return true;
        }

        private static bool AtualizarPlacar(
            int? novoValor,
            bool limparQuandoNulo,
            Func<int?> obterAtual,
            Action<int?> atualizar)
        {
            if (novoValor.HasValue)
            {
                if (obterAtual() == novoValor)
                {
                    return false;
                }

                atualizar(novoValor);
                return true;
            }

            if (!limparQuandoNulo || !obterAtual().HasValue)
            {
                return false;
            }

            atualizar(null);
            return true;
        }

        private static string MontarMensagemSincronizacao(
            CampeonatoSincronizacaoResultado resultado,
            string prefixo)
        {
            var mensagem = $"{prefixo}. Jogos criados: {resultado.JogosCriados}; jogos atualizados: {resultado.JogosAtualizados}; " +
                $"times criados: {resultado.TimesCriados}; times atualizados: {resultado.TimesAtualizados}; " +
                $"jogos ignorados: {resultado.JogosIgnorados}; erros: {resultado.Erros.Count}.";

            mensagem += resultado.ClassificacaoRecalculada
                ? $" Classificação recalculada: sim ({resultado.DataRecalculo:dd/MM/yyyy HH:mm})."
                : " Classificação recalculada: não.";

            return mensagem;
        }

        private static int? ExtrairNumeroRodada(string? rodada)
        {
            if (string.IsNullOrWhiteSpace(rodada))
            {
                return null;
            }

            var match = Regex.Match(rodada, @"(?<!\d)(\d{1,3})(?!\d)");

            return match.Success && int.TryParse(match.Groups[1].Value, out var numero)
                ? numero
                : null;
        }

        private static string? ExtrairGrupo(string? rodada)
        {
            if (string.IsNullOrWhiteSpace(rodada))
            {
                return null;
            }

            var match = Regex.Match(rodada, @"(?:Group|Grupo)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value.ToUpperInvariant()
                : null;
        }

        private static string? ObterString(JsonElement element, string propriedade)
        {
            return element.TryGetProperty(propriedade, out var valor) && valor.ValueKind != JsonValueKind.Null
                ? valor.GetString()
                : null;
        }

        private static int ObterInt(JsonElement element, string propriedade)
        {
            return element.TryGetProperty(propriedade, out var valor) && valor.ValueKind != JsonValueKind.Null
                ? valor.GetInt32()
                : 0;
        }

        private static int? ObterIntNullable(JsonElement element, string propriedade)
        {
            return element.TryGetProperty(propriedade, out var valor) && valor.ValueKind != JsonValueKind.Null
                ? valor.GetInt32()
                : null;
        }

        private static bool? ObterBoolNullable(JsonElement element, string propriedade)
        {
            return element.TryGetProperty(propriedade, out var valor) &&
                   valor.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? valor.GetBoolean()
                : null;
        }
    }

    internal class ApiFixtureData
    {
        public int ApiFixtureId { get; set; }

        public DateTime DataJogo { get; set; }

        public string Status { get; set; } = "Agendado";

        public string StatusApi { get; set; } = string.Empty;

        public string? Fase { get; set; }

        public string? Grupo { get; set; }

        public int? Rodada { get; set; }

        public int? GolsCasa { get; set; }

        public int? GolsVisitante { get; set; }

        public ApiFixtureTime TimeCasa { get; set; } = new();

        public ApiFixtureTime TimeVisitante { get; set; } = new();

        public bool EstaFinalizado => string.Equals(Status, "Finalizado", StringComparison.OrdinalIgnoreCase);

        public bool NaoComecou =>
            string.Equals(Status, "Agendado", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, "Adiado", StringComparison.OrdinalIgnoreCase);
    }

    internal class ApiFixtureTime
    {
        public int ApiTeamId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? EscudoUrl { get; set; }

        public string Tipo { get; set; } = "Clube";
    }

    public class CampeonatoSincronizacaoResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public int? CampeonatoId { get; set; }

        public int TotalProcessados { get; set; }

        public int JogosCriados { get; set; }

        public int JogosAtualizados { get; set; }

        public int JogosIgnorados { get; set; }

        public int JogosFinalizados { get; set; }

        public int TimesCriados { get; set; }

        public int TimesAtualizados { get; set; }

        public bool ClassificacaoRecalculada { get; set; }

        public DateTime? DataRecalculo { get; set; }

        public bool RedirecionarParaPortal { get; set; }

        public List<string> Erros { get; set; } = new();

        public static CampeonatoSincronizacaoResultado Iniciar(
            int campeonatoId,
            bool redirecionarParaPortal = false)
        {
            return new CampeonatoSincronizacaoResultado
            {
                Sucesso = true,
                CampeonatoId = campeonatoId,
                RedirecionarParaPortal = redirecionarParaPortal
            };
        }

        public static CampeonatoSincronizacaoResultado Falha(
            string mensagem,
            int? campeonatoId = null,
            bool redirecionarParaPortal = false)
        {
            return new CampeonatoSincronizacaoResultado
            {
                Sucesso = false,
                Mensagem = mensagem,
                CampeonatoId = campeonatoId,
                RedirecionarParaPortal = redirecionarParaPortal,
                Erros = new List<string> { mensagem }
            };
        }
    }
}
