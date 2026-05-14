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
    }
}
