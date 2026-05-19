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
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;
        private readonly ILogger<CampeonatoSincronizacaoService> _logger;

        public CampeonatoSincronizacaoService(
            FootballApiService footballApiService,
            AppDbContext context,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService,
            ILogger<CampeonatoSincronizacaoService> logger)
        {
            _footballApiService = footballApiService;
            _context = context;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
            _logger = logger;
        }

        public async Task<CampeonatoSincronizacaoResultado> AtualizarResultadosAsync(int campeonatoId)
        {
            _logger.LogInformation("Iniciando atualizacao de resultados. CampeonatoId: {CampeonatoId}", campeonatoId);

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                _logger.LogWarning("Campeonato nao encontrado. CampeonatoId: {CampeonatoId}", campeonatoId);
                return CampeonatoSincronizacaoResultado.Falha("Campeonato não encontrado.");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                _logger.LogWarning("Campeonato sem ApiLeagueId. CampeonatoId: {CampeonatoId}", campeonatoId);
                return CampeonatoSincronizacaoResultado.Falha("Este campeonato não possui ID da API.");
            }

            try
            {
                int jogosAtualizados = await AtualizarResultadosCampeonatoAsync(campeonato);

                var mensagem = $"Resultados atualizados com sucesso. Jogos atualizados: {jogosAtualizados}.";

                if (jogosAtualizados > 0)
                {
                    await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
                    await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);
                    mensagem += " Classificação e palpites recalculados.";
                }

                _logger.LogInformation("Resultados atualizados com sucesso. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return CampeonatoSincronizacaoResultado.Ok(
                    mensagem,
                    campeonato.Id,
                    jogosAtualizados
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar resultados. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return CampeonatoSincronizacaoResultado.Falha($"Erro ao atualizar resultados: {ex.Message}");
            }
        }

        public async Task<CampeonatoSincronizacaoResultado> SincronizarCampeonatoAsync(int campeonatoId)
        {
            _logger.LogInformation("Iniciando sincronizacao de campeonato. CampeonatoId: {CampeonatoId}", campeonatoId);

            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                _logger.LogWarning("Campeonato nao encontrado. CampeonatoId: {CampeonatoId}", campeonatoId);
                return CampeonatoSincronizacaoResultado.Falha("Campeonato não encontrado.");
            }

            if (!campeonato.ApiLeagueId.HasValue)
            {
                _logger.LogWarning("Campeonato sem ApiLeagueId. CampeonatoId: {CampeonatoId}", campeonatoId);
                return CampeonatoSincronizacaoResultado.Falha("Este campeonato não possui ID da API.");
            }

            try
            {
                int jogosAtualizados = await AtualizarResultadosCampeonatoAsync(campeonato);
                await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
                await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);

                _logger.LogInformation("Sincronizacao concluida com sucesso. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return CampeonatoSincronizacaoResultado.Ok(
                    $"Sincronização concluída. Jogos atualizados: {jogosAtualizados}. Classificação e palpites recalculados.",
                    campeonato.Id,
                    jogosAtualizados,
                    redirecionarParaPortal: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar campeonato. CampeonatoId: {CampeonatoId}", campeonato.Id);

                return CampeonatoSincronizacaoResultado.Falha(
                    $"Erro ao sincronizar campeonato: {ex.Message}",
                    campeonato.Id,
                    redirecionarParaPortal: true
                );
            }
        }

        private async Task<int> AtualizarResultadosCampeonatoAsync(Campeonato campeonato)
        {
            using var resultado = await _footballApiService.BuscarJogosAsync(
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
                    var league = item.GetProperty("league");

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

                    string status = FootballApiStatusMapper.ConverterStatusJogo(statusApi);

                    string? rodada = league.TryGetProperty("round", out var roundElement)
                        ? roundElement.GetString()
                        : null;

                    int? golsCasa = null;
                    int? golsVisitante = null;

                    if (goals.TryGetProperty("home", out var golsCasaElement) &&
                        golsCasaElement.ValueKind != JsonValueKind.Null)
                    {
                        golsCasa = golsCasaElement.GetInt32();
                    }

                    if (goals.TryGetProperty("away", out var golsVisitanteElement) &&
                        golsVisitanteElement.ValueKind != JsonValueKind.Null)
                    {
                        golsVisitante = golsVisitanteElement.GetInt32();
                    }

                    jogo.DataJogo = dataJogo;
                    jogo.Status = status;
                    jogo.GolsCasa = golsCasa;
                    jogo.GolsVisitante = golsVisitante;

                    if (!string.IsNullOrWhiteSpace(rodada))
                    {
                        jogo.Fase = rodada;
                        jogo.Rodada = ExtrairNumeroRodada(rodada) ?? jogo.Rodada;
                        jogo.Grupo = ExtrairGrupo(rodada) ?? jogo.Grupo;
                    }

                    _context.Jogos.Update(jogo);
                    jogosAtualizados++;
                }

                await _context.SaveChangesAsync();
            }

            return jogosAtualizados;
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
    }

    public class CampeonatoSincronizacaoResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public int? CampeonatoId { get; set; }

        public int JogosAtualizados { get; set; }

        public bool RedirecionarParaPortal { get; set; }

        public static CampeonatoSincronizacaoResultado Ok(
            string mensagem,
            int campeonatoId,
            int jogosAtualizados,
            bool redirecionarParaPortal = false)
        {
            return new CampeonatoSincronizacaoResultado
            {
                Sucesso = true,
                Mensagem = mensagem,
                CampeonatoId = campeonatoId,
                JogosAtualizados = jogosAtualizados,
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
                RedirecionarParaPortal = redirecionarParaPortal
            };
        }
    }
}
