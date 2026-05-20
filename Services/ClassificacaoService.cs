using FutPlay.Data;
using FutPlay.Models;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class ClassificacaoService
    {
        private readonly AppDbContext _context;

        public ClassificacaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RecalcularClassificacaoCampeonatoAsync(int campeonatoId)
        {
            var campeonato = await _context.Campeonatos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);

            if (campeonato == null)
            {
                return;
            }

            var classificarPorGrupo = campeonato.UsaClassificacaoPorGrupos;

            var jogosDoCampeonato = await _context.Jogos
                .Where(j =>
                    j.CampeonatoId == campeonatoId &&
                    j.Ativo)
                .ToListAsync();

            var jogosFinalizados = jogosDoCampeonato
                .Where(j =>
                    string.Equals(j.Status, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                    j.GolsCasa.HasValue &&
                    j.GolsVisitante.HasValue)
                .ToList();

            var campeonatoTimes = classificarPorGrupo
                ? await _context.CampeonatoTimes
                    .Include(ct => ct.Grupo)
                    .Where(ct =>
                        ct.CampeonatoId == campeonatoId &&
                        ct.Ativo)
                    .ToListAsync()
                : new List<CampeonatoTime>();

            var gruposPorTime = campeonatoTimes
                .GroupBy(ct => ct.TimeId)
                .ToDictionary(
                    g => g.Key,
                    g => NormalizarGrupo(g.First().Grupo?.Nome));

            var classificacoesAtuais = await _context.Classificacoes
                .Where(c => c.CampeonatoId == campeonatoId)
                .ToListAsync();

            _context.Classificacoes.RemoveRange(classificacoesAtuais);

            var tabela = new Dictionary<string, Classificacao>();

            if (classificarPorGrupo && campeonatoTimes.Any())
            {
                foreach (var campeonatoTime in campeonatoTimes)
                {
                    var grupo = NormalizarGrupo(campeonatoTime.Grupo?.Nome);
                    ObterOuCriarClassificacao(tabela, campeonatoId, campeonatoTime.TimeId, grupo);
                }
            }
            else
            {
                foreach (var jogo in jogosDoCampeonato)
                {
                    var grupo = classificarPorGrupo ? NormalizarGrupo(jogo.Grupo) : null;

                    ObterOuCriarClassificacao(tabela, campeonatoId, jogo.TimeCasaId, grupo);
                    ObterOuCriarClassificacao(tabela, campeonatoId, jogo.TimeVisitanteId, grupo);
                }
            }

            foreach (var jogo in jogosFinalizados)
            {
                var grupoCasa = classificarPorGrupo
                    ? ObterGrupoClassificacao(jogo.TimeCasaId, jogo.Grupo, gruposPorTime)
                    : null;
                var grupoVisitante = classificarPorGrupo
                    ? ObterGrupoClassificacao(jogo.TimeVisitanteId, jogo.Grupo, gruposPorTime)
                    : null;

                var casa = ObterOuCriarClassificacao(tabela, campeonatoId, jogo.TimeCasaId, grupoCasa);
                var visitante = ObterOuCriarClassificacao(tabela, campeonatoId, jogo.TimeVisitanteId, grupoVisitante);

                int golsCasa = jogo.GolsCasa.GetValueOrDefault();
                int golsVisitante = jogo.GolsVisitante.GetValueOrDefault();

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

            var classificacoesOrdenadas = tabela
                .Select(item => new
                {
                    Grupo = ObterGrupoDaChave(item.Key),
                    Classificacao = item.Value
                })
                .OrderBy(item => string.IsNullOrWhiteSpace(item.Grupo) ? "Z" : item.Grupo)
                .ThenByDescending(item => item.Classificacao.Pontos)
                .ThenByDescending(item => item.Classificacao.Vitorias)
                .ThenByDescending(item => item.Classificacao.SaldoGols)
                .ThenByDescending(item => item.Classificacao.GolsPro)
                .ThenBy(item => item.Classificacao.TimeId)
                .ToList();

            var grupos = classificacoesOrdenadas
                .GroupBy(item => string.IsNullOrWhiteSpace(item.Grupo) ? "" : item.Grupo);

            foreach (var grupo in grupos)
            {
                int posicao = 1;

                foreach (var item in grupo)
                {
                    item.Classificacao.Posicao = posicao;
                    posicao++;
                }
            }

            _context.Classificacoes.AddRange(classificacoesOrdenadas.Select(item => item.Classificacao));

            await _context.SaveChangesAsync();
        }

        private Classificacao CriarClassificacaoInicial(int campeonatoId, int timeId, string? grupo)
        {
            return new Classificacao
            {
                CampeonatoId = campeonatoId,
                TimeId = timeId,
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

        private Classificacao ObterOuCriarClassificacao(
            Dictionary<string, Classificacao> tabela,
            int campeonatoId,
            int timeId,
            string? grupo)
        {
            var grupoNormalizado = NormalizarGrupo(grupo);
            var chave = CriarChaveClassificacao(grupoNormalizado, timeId);

            if (!tabela.TryGetValue(chave, out var classificacao))
            {
                classificacao = CriarClassificacaoInicial(campeonatoId, timeId, grupoNormalizado);
                tabela[chave] = classificacao;
            }

            return classificacao;
        }

        private static string? ObterGrupoClassificacao(
            int timeId,
            string? grupoJogo,
            Dictionary<int, string?> gruposPorTime)
        {
            if (gruposPorTime.TryGetValue(timeId, out var grupoTime) &&
                !string.IsNullOrWhiteSpace(grupoTime))
            {
                return NormalizarGrupo(grupoTime);
            }

            return NormalizarGrupo(grupoJogo);
        }

        private static string? NormalizarGrupo(string? grupo)
        {
            return string.IsNullOrWhiteSpace(grupo)
                ? null
                : grupo.Trim();
        }

        private static string CriarChaveClassificacao(string? grupo, int timeId)
        {
            return $"{NormalizarGrupo(grupo) ?? string.Empty}|{timeId}";
        }

        private static string? ObterGrupoDaChave(string chave)
        {
            var indiceSeparador = chave.IndexOf('|');

            if (indiceSeparador <= 0)
            {
                return null;
            }

            return NormalizarGrupo(chave.Substring(0, indiceSeparador));
        }

    }
}
