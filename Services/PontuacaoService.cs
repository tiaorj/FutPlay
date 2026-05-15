using FutPlay.Data;
using FutPlay.Models;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class PontuacaoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PontuacaoService> _logger;

        public PontuacaoService(AppDbContext context, ILogger<PontuacaoService> logger)
        {
            _context = context;
            _logger = logger;

        }

        public int CalcularPontuacao(Palpite palpite, Jogo jogo)
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

        public async Task RecalcularPontuacaoPalpitesCampeonatoAsync(int campeonatoId)
        {
            _logger.LogInformation(
    "Iniciando recálculo de pontuação dos palpites do campeonato {CampeonatoId}",
    campeonatoId);

            try
            {
                var palpites = await _context.Palpites
                    .Include(p => p.Jogo)
                    .Where(p =>
                        p.Ativo &&
                        p.Jogo != null &&
                        p.Jogo.CampeonatoId == campeonatoId)
                    .ToListAsync();

                _logger.LogInformation(
                    "Foram encontrados {TotalPalpites} palpites para recalcular no campeonato {CampeonatoId}",
                    palpites.Count,
                    campeonatoId);

                foreach (var palpite in palpites)
                {
                    AtualizarPontuacaoPalpite(palpite);
                }

                await _context.SaveChangesAsync();

                var participantesAfetados = palpites
                    .Select(p => p.LigaParticipanteId)
                    .Distinct()
                    .ToList();

                await AtualizarPontuacaoParticipantesAsync(participantesAfetados);

                _logger.LogInformation(
                    "Recálculo de pontuação concluído para o campeonato {CampeonatoId}",
                    campeonatoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao recalcular pontuação dos palpites do campeonato {CampeonatoId}",
                    campeonatoId);

                throw;
            }
        }

        public async Task RecalcularPontuacaoPalpitesAsync()
        {
            var palpites = await _context.Palpites
                .Include(p => p.Jogo)
                .Where(p => p.Ativo)
                .ToListAsync();

            foreach (var palpite in palpites)
            {
                AtualizarPontuacaoPalpite(palpite);
            }

            await _context.SaveChangesAsync();

            await AtualizarPontuacaoTodosParticipantesAsync();
        }

        public async Task AtualizarPontuacaoParticipantesAsync(IEnumerable<int> participanteIds)
        {
            var ids = participanteIds
                .Distinct()
                .ToList();

            foreach (var participanteId in ids)
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

        public async Task AtualizarPontuacaoTodosParticipantesAsync()
        {
            var participantes = await _context.LigaParticipantes.ToListAsync();

            foreach (var participante in participantes)
            {
                participante.PontuacaoTotal = await _context.Palpites
                    .Where(p =>
                        p.LigaParticipanteId == participante.Id &&
                        p.LigaId == participante.LigaId &&
                        p.Ativo)
                    .SumAsync(p => p.PontosGanhos);
            }

            await _context.SaveChangesAsync();
        }

        private void AtualizarPontuacaoPalpite(Palpite palpite)
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
