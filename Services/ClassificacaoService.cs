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

            var classificacoesAtuais = await _context.Classificacoes
                .Where(c => c.CampeonatoId == campeonatoId)
                .ToListAsync();

            _context.Classificacoes.RemoveRange(classificacoesAtuais);

            var tabela = new Dictionary<string, Classificacao>();

            foreach (var jogo in jogosDoCampeonato)
            {
                var grupo = classificarPorGrupo ? jogo.Grupo : null;

                var chaveCasa = CriarChaveClassificacao(grupo, jogo.TimeCasaId);
                var chaveVisitante = CriarChaveClassificacao(grupo, jogo.TimeVisitanteId);

                if (!tabela.ContainsKey(chaveCasa))
                {
                    tabela[chaveCasa] = CriarClassificacaoInicial(campeonatoId, jogo.TimeCasaId, grupo);
                }

                if (!tabela.ContainsKey(chaveVisitante))
                {
                    tabela[chaveVisitante] = CriarClassificacaoInicial(campeonatoId, jogo.TimeVisitanteId, grupo);
                }
            }

            foreach (var jogo in jogosFinalizados)
            {
                var grupo = classificarPorGrupo ? jogo.Grupo : null;

                var chaveCasa = CriarChaveClassificacao(grupo, jogo.TimeCasaId);
                var chaveVisitante = CriarChaveClassificacao(grupo, jogo.TimeVisitanteId);

                var casa = tabela[chaveCasa];
                var visitante = tabela[chaveVisitante];

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

            var classificacoesOrdenadas = tabela.Values
                .OrderBy(c => string.IsNullOrWhiteSpace(c.Grupo) ? "Z" : c.Grupo)
                .ThenByDescending(c => c.Pontos)
                .ThenByDescending(c => c.Vitorias)
                .ThenByDescending(c => c.SaldoGols)
                .ThenByDescending(c => c.GolsPro)
                .ThenBy(c => c.TimeId)
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
        private static string CriarChaveClassificacao(string? grupo, int timeId)
        {
            return $"{grupo ?? string.Empty}|{timeId}";
        }

    }
}
