using FutPlay.Data;
using FutPlay.Models;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class MockDataService
    {
        private readonly AppDbContext _context;
        private readonly ClassificacaoService _classificacaoService;
        private readonly PontuacaoService _pontuacaoService;

        private const string NomeCampeonatoMock = "Copa FutPlay Mock";
        private const string NomeLigaMock = "Liga Pública FutPlay Mock";

        public MockDataService(
            AppDbContext context,
            ClassificacaoService classificacaoService,
            PontuacaoService pontuacaoService)
        {
            _context = context;
            _classificacaoService = classificacaoService;
            _pontuacaoService = pontuacaoService;
        }

        public async Task<bool> DadosTesteExistemAsync()
        {
            return await _context.Campeonatos
                .AnyAsync(c => c.Nome == NomeCampeonatoMock);
        }

        public async Task<string> GerarDadosTesteAsync()
        {
            var jaExiste = await DadosTesteExistemAsync();

            if (jaExiste)
            {
                return "Os dados de teste já foram gerados anteriormente. Limpe os dados de teste antes de gerar novamente.";
            }

            var campeonato = new Campeonato
            {
                Nome = NomeCampeonatoMock,
                Ano = DateTime.Now.Year,
                Tipo = "Copa Teste",
                Pais = "Mundo",
                LogoUrl = null,
                DataInicio = DateTime.Today.AddDays(-10),
                DataFim = DateTime.Today.AddDays(30),
                Ativo = true,
                ApiLeagueId = null
            };

            _context.Campeonatos.Add(campeonato);
            await _context.SaveChangesAsync();

            var times = new List<Time>
            {
                new Time { Nome = "Brasil Mock", Sigla = "BRA", Pais = "Brasil", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "Argentina Mock", Sigla = "ARG", Pais = "Argentina", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "França Mock", Sigla = "FRA", Pais = "França", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "Alemanha Mock", Sigla = "ALE", Pais = "Alemanha", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "Espanha Mock", Sigla = "ESP", Pais = "Espanha", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "Itália Mock", Sigla = "ITA", Pais = "Itália", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "Portugal Mock", Sigla = "POR", Pais = "Portugal", Tipo = "Seleção", Ativo = true },
                new Time { Nome = "Inglaterra Mock", Sigla = "ING", Pais = "Inglaterra", Tipo = "Seleção", Ativo = true }
            };

            _context.Times.AddRange(times);
            await _context.SaveChangesAsync();

            var brasil = times.First(t => t.Sigla == "BRA");
            var argentina = times.First(t => t.Sigla == "ARG");
            var franca = times.First(t => t.Sigla == "FRA");
            var alemanha = times.First(t => t.Sigla == "ALE");
            var espanha = times.First(t => t.Sigla == "ESP");
            var italia = times.First(t => t.Sigla == "ITA");
            var portugal = times.First(t => t.Sigla == "POR");
            var inglaterra = times.First(t => t.Sigla == "ING");

            var jogos = new List<Jogo>
            {
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = brasil.Id,
                    TimeVisitanteId = argentina.Id,
                    DataJogo = DateTime.Now.AddDays(-8),
                    Fase = "Fase de Grupos",
                    Grupo = "A",
                    Rodada = 1,
                    GolsCasa = 2,
                    GolsVisitante = 1,
                    Status = "Finalizado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = franca.Id,
                    TimeVisitanteId = alemanha.Id,
                    DataJogo = DateTime.Now.AddDays(-7),
                    Fase = "Fase de Grupos",
                    Grupo = "A",
                    Rodada = 1,
                    GolsCasa = 1,
                    GolsVisitante = 1,
                    Status = "Finalizado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = brasil.Id,
                    TimeVisitanteId = franca.Id,
                    DataJogo = DateTime.Now.AddDays(-5),
                    Fase = "Fase de Grupos",
                    Grupo = "A",
                    Rodada = 2,
                    GolsCasa = 3,
                    GolsVisitante = 0,
                    Status = "Finalizado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = argentina.Id,
                    TimeVisitanteId = alemanha.Id,
                    DataJogo = DateTime.Now.AddDays(2),
                    Fase = "Fase de Grupos",
                    Grupo = "A",
                    Rodada = 2,
                    Status = "Agendado",
                    Ativo = true
                },

                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = espanha.Id,
                    TimeVisitanteId = italia.Id,
                    DataJogo = DateTime.Now.AddDays(-6),
                    Fase = "Fase de Grupos",
                    Grupo = "B",
                    Rodada = 1,
                    GolsCasa = 0,
                    GolsVisitante = 0,
                    Status = "Finalizado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = portugal.Id,
                    TimeVisitanteId = inglaterra.Id,
                    DataJogo = DateTime.Now.AddDays(-4),
                    Fase = "Fase de Grupos",
                    Grupo = "B",
                    Rodada = 1,
                    GolsCasa = 2,
                    GolsVisitante = 2,
                    Status = "Finalizado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = espanha.Id,
                    TimeVisitanteId = portugal.Id,
                    DataJogo = DateTime.Now.AddDays(3),
                    Fase = "Fase de Grupos",
                    Grupo = "B",
                    Rodada = 2,
                    Status = "Agendado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = italia.Id,
                    TimeVisitanteId = inglaterra.Id,
                    DataJogo = DateTime.Now.AddDays(4),
                    Fase = "Fase de Grupos",
                    Grupo = "B",
                    Rodada = 2,
                    Status = "Agendado",
                    Ativo = true
                },

                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = brasil.Id,
                    TimeVisitanteId = espanha.Id,
                    DataJogo = DateTime.Now.AddDays(8),
                    Fase = "Semifinal",
                    Grupo = null,
                    Rodada = null,
                    Status = "Agendado",
                    Ativo = true
                },
                new Jogo
                {
                    CampeonatoId = campeonato.Id,
                    TimeCasaId = franca.Id,
                    TimeVisitanteId = portugal.Id,
                    DataJogo = DateTime.Now.AddDays(9),
                    Fase = "Semifinal",
                    Grupo = null,
                    Rodada = null,
                    Status = "Agendado",
                    Ativo = true
                }
            };

            _context.Jogos.AddRange(jogos);
            await _context.SaveChangesAsync();

            var ligaPublica = new Liga
            {
                Nome = NomeLigaMock,
                CampeonatoId = campeonato.Id,
                CodigoConvite = "MOCK01",
                Publica = true,
                DataCriacao = DateTime.Now,
                Ativo = true
            };

            var ligaPrivada = new Liga
            {
                Nome = "Liga Privada FutPlay Mock",
                CampeonatoId = campeonato.Id,
                CodigoConvite = "MOCK02",
                Publica = false,
                DataCriacao = DateTime.Now,
                Ativo = true
            };

            _context.Ligas.AddRange(ligaPublica, ligaPrivada);
            await _context.SaveChangesAsync();

            var participantes = new List<LigaParticipante>
            {
                new LigaParticipante
                {
                    LigaId = ligaPublica.Id,
                    Nome = "Sebastião Oliveira",
                    Email = "sebastiao.mock@futplay.com",
                    DataEntrada = DateTime.Now,
                    Ativo = true
                },
                new LigaParticipante
                {
                    LigaId = ligaPublica.Id,
                    Nome = "João Silva",
                    Email = "joao.mock@futplay.com",
                    DataEntrada = DateTime.Now,
                    Ativo = true
                },
                new LigaParticipante
                {
                    LigaId = ligaPublica.Id,
                    Nome = "Maria Souza",
                    Email = "maria.mock@futplay.com",
                    DataEntrada = DateTime.Now,
                    Ativo = true
                },
                new LigaParticipante
                {
                    LigaId = ligaPublica.Id,
                    Nome = "Carlos Lima",
                    Email = "carlos.mock@futplay.com",
                    DataEntrada = DateTime.Now,
                    Ativo = true
                }
            };

            _context.LigaParticipantes.AddRange(participantes);
            await _context.SaveChangesAsync();

            var jogosFinalizados = jogos
                .Where(j => j.Status == "Finalizado")
                .ToList();

            var palpites = new List<Palpite>();

            foreach (var participante in participantes)
            {
                foreach (var jogo in jogosFinalizados)
                {
                    var golsCasa = jogo.GolsCasa ?? 0;
                    var golsVisitante = jogo.GolsVisitante ?? 0;

                    if (participante.Nome.StartsWith("Sebastião"))
                    {
                        palpites.Add(CriarPalpite(ligaPublica.Id, participante.Id, jogo.Id, golsCasa, golsVisitante));
                    }
                    else if (participante.Nome.StartsWith("João"))
                    {
                        palpites.Add(CriarPalpite(ligaPublica.Id, participante.Id, jogo.Id, Math.Max(golsCasa - 1, 0), golsVisitante));
                    }
                    else if (participante.Nome.StartsWith("Maria"))
                    {
                        palpites.Add(CriarPalpite(ligaPublica.Id, participante.Id, jogo.Id, golsCasa, Math.Max(golsVisitante - 1, 0)));
                    }
                    else
                    {
                        palpites.Add(CriarPalpite(ligaPublica.Id, participante.Id, jogo.Id, golsVisitante, golsCasa));
                    }
                }
            }

            _context.Palpites.AddRange(palpites);
            await _context.SaveChangesAsync();

            await _classificacaoService.RecalcularClassificacaoCampeonatoAsync(campeonato.Id);
            await _pontuacaoService.RecalcularPontuacaoPalpitesCampeonatoAsync(campeonato.Id);

            return "Dados de teste gerados com sucesso.";
        }

        public async Task<string> LimparDadosTesteAsync()
        {
            var campeonato = await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Nome == NomeCampeonatoMock);

            if (campeonato == null)
            {
                return "Nenhum dado de teste encontrado para limpar.";
            }

            var ligas = await _context.Ligas
                .Where(l => l.CampeonatoId == campeonato.Id)
                .ToListAsync();

            var ligaIds = ligas.Select(l => l.Id).ToList();

            var participantes = await _context.LigaParticipantes
                .Where(p => ligaIds.Contains(p.LigaId))
                .ToListAsync();

            var participanteIds = participantes.Select(p => p.Id).ToList();

            var palpites = await _context.Palpites
                .Where(p =>
                    ligaIds.Contains(p.LigaId) ||
                    participanteIds.Contains(p.LigaParticipanteId))
                .ToListAsync();

            var classificacoes = await _context.Classificacoes
                .Where(c => c.CampeonatoId == campeonato.Id)
                .ToListAsync();

            var jogos = await _context.Jogos
                .Where(j => j.CampeonatoId == campeonato.Id)
                .ToListAsync();

            var timeIds = jogos
                .SelectMany(j => new[] { j.TimeCasaId, j.TimeVisitanteId })
                .Distinct()
                .ToList();

            var timesMock = await _context.Times
                .Where(t => timeIds.Contains(t.Id) && t.Nome.Contains("Mock"))
                .ToListAsync();

            _context.Palpites.RemoveRange(palpites);
            _context.LigaParticipantes.RemoveRange(participantes);
            _context.Ligas.RemoveRange(ligas);
            _context.Classificacoes.RemoveRange(classificacoes);
            _context.Jogos.RemoveRange(jogos);
            _context.Times.RemoveRange(timesMock);
            _context.Campeonatos.Remove(campeonato);

            await _context.SaveChangesAsync();

            return "Dados de teste removidos com sucesso.";
        }

        private Palpite CriarPalpite(
            int ligaId,
            int participanteId,
            int jogoId,
            int golsCasa,
            int golsVisitante)
        {
            return new Palpite
            {
                LigaId = ligaId,
                LigaParticipanteId = participanteId,
                JogoId = jogoId,
                GolsCasaPalpite = golsCasa,
                GolsVisitantePalpite = golsVisitante,
                DataPalpite = DateTime.Now.AddDays(-9),
                PontosGanhos = 0,
                Ativo = true
            };
        }
    }
}