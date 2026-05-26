using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FutPlay.Services
{
    public class PalpiteComunidadeService
    {
        public const string ResultadoCasa = "Casa";
        public const string ResultadoEmpate = "Empate";
        public const string ResultadoVisitante = "Visitante";

        private readonly AppDbContext _context;
        private readonly AppTimeService _appTimeService;
        private readonly PalpiteBloqueioService _palpiteBloqueioService;

        public PalpiteComunidadeService(
            AppDbContext context,
            AppTimeService appTimeService,
            PalpiteBloqueioService palpiteBloqueioService)
        {
            _context = context;
            _appTimeService = appTimeService;
            _palpiteBloqueioService = palpiteBloqueioService;
        }

        public async Task<PalpiteComunidadeResumoViewModel> MontarResumoAsync(
            Jogo jogo,
            string? usuarioId)
        {
            var palpites = await _context.PalpitesComunidade
                .AsNoTracking()
                .Where(p => p.JogoId == jogo.Id)
                .ToListAsync();

            var palpiteUsuario = string.IsNullOrWhiteSpace(usuarioId)
                ? null
                : palpites.FirstOrDefault(p => p.UsuarioId == usuarioId);

            var total = palpites.Count;
            var totalCasa = palpites.Count(p => p.ResultadoPrevisto == ResultadoCasa);
            var totalEmpate = palpites.Count(p => p.ResultadoPrevisto == ResultadoEmpate);
            var totalVisitante = palpites.Count(p => p.ResultadoPrevisto == ResultadoVisitante);
            var placares = MontarDistribuicaoPlacares(palpites);
            var bloqueado = _palpiteBloqueioService.PalpiteBloqueado(jogo);
            var jogoFinalizado = JogoFinalizado(jogo);
            var resultadoReal = jogoFinalizado
                ? ObterResultado(jogo.GolsCasa!.Value, jogo.GolsVisitante!.Value)
                : null;

            return new PalpiteComunidadeResumoViewModel
            {
                JogoId = jogo.Id,
                TotalPalpites = total,
                TotalCasa = totalCasa,
                TotalEmpate = totalEmpate,
                TotalVisitante = totalVisitante,
                PercentualCasa = CalcularPercentual(totalCasa, total),
                PercentualEmpate = CalcularPercentual(totalEmpate, total),
                PercentualVisitante = CalcularPercentual(totalVisitante, total),
                PlacarMaisEscolhido = placares.FirstOrDefault()?.Placar,
                TotalPlacarMaisEscolhido = placares.FirstOrDefault()?.Total ?? 0,
                PlacaresMaisEscolhidos = placares,
                PalpiteUsuario = palpiteUsuario == null ? null : MontarPalpiteUsuario(palpiteUsuario),
                Bloqueado = bloqueado,
                DataBloqueio = _palpiteBloqueioService.ObterDataBloqueio(jogo),
                MensagemBloqueio = "Palpites bloqueados 5 minutos antes da partida.",
                UsuarioPodePalpitar = !string.IsNullOrWhiteSpace(usuarioId) && !bloqueado,
                JogoFinalizado = jogoFinalizado,
                ResultadoReal = resultadoReal,
                UsuarioAcertouResultado = jogoFinalizado && palpiteUsuario != null
                    ? palpiteUsuario.ResultadoPrevisto == resultadoReal
                    : null,
                UsuarioAcertouPlacarExato = jogoFinalizado && palpiteUsuario != null
                    ? palpiteUsuario.GolsCasaPalpite == jogo.GolsCasa &&
                      palpiteUsuario.GolsVisitantePalpite == jogo.GolsVisitante
                    : null
            };
        }

        public async Task<PalpiteComunidadeSalvarResultado> SalvarAsync(
            PalpiteComunidadeViewModel model,
            string usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Falha("Entre para deixar seu palpite.");
            }

            var jogo = await _context.Jogos
                .FirstOrDefaultAsync(j => j.Id == model.JogoId && j.Ativo);

            if (jogo == null)
            {
                return Falha("Jogo não encontrado.");
            }

            if (_palpiteBloqueioService.PalpiteBloqueado(jogo))
            {
                return Falha("Palpites bloqueados 5 minutos antes da partida.");
            }

            var resultado = NormalizarResultado(model.ResultadoPrevisto);

            if (string.IsNullOrWhiteSpace(resultado))
            {
                return Falha("Escolha vitória do mandante, empate ou vitória do visitante.");
            }

            if (!PlacarValido(model.GolsCasaPalpite, model.GolsVisitantePalpite, out var mensagemPlacar))
            {
                return Falha(mensagemPlacar);
            }

            var agora = _appTimeService.Agora;
            var palpite = await _context.PalpitesComunidade
                .FirstOrDefaultAsync(p => p.JogoId == model.JogoId && p.UsuarioId == usuarioId);

            if (palpite == null)
            {
                palpite = new PalpiteComunidade
                {
                    JogoId = model.JogoId,
                    UsuarioId = usuarioId,
                    CriadoEm = agora
                };

                _context.PalpitesComunidade.Add(palpite);
            }

            palpite.ResultadoPrevisto = resultado;
            palpite.GolsCasaPalpite = model.GolsCasaPalpite;
            palpite.GolsVisitantePalpite = model.GolsVisitantePalpite;
            palpite.AtualizadoEm = agora;

            await _context.SaveChangesAsync();

            return new PalpiteComunidadeSalvarResultado
            {
                Sucesso = true,
                Mensagem = "Palpite da comunidade salvo com sucesso."
            };
        }

        private static PalpiteComunidadeSalvarResultado Falha(string mensagem)
        {
            return new PalpiteComunidadeSalvarResultado
            {
                Sucesso = false,
                Mensagem = mensagem
            };
        }

        private static PalpiteComunidadeUsuarioViewModel MontarPalpiteUsuario(PalpiteComunidade palpite)
        {
            return new PalpiteComunidadeUsuarioViewModel
            {
                ResultadoPrevisto = palpite.ResultadoPrevisto,
                ResultadoTexto = TextoResultado(palpite.ResultadoPrevisto),
                GolsCasaPalpite = palpite.GolsCasaPalpite,
                GolsVisitantePalpite = palpite.GolsVisitantePalpite,
                PlacarTexto = palpite.GolsCasaPalpite.HasValue && palpite.GolsVisitantePalpite.HasValue
                    ? $"{palpite.GolsCasaPalpite} x {palpite.GolsVisitantePalpite}"
                    : null,
                AtualizadoEm = palpite.AtualizadoEm
            };
        }

        private static List<PalpiteComunidadePlacarViewModel> MontarDistribuicaoPlacares(
            List<PalpiteComunidade> palpites)
        {
            var palpitesComPlacar = palpites
                .Where(p => p.GolsCasaPalpite.HasValue && p.GolsVisitantePalpite.HasValue)
                .ToList();

            var totalComPlacar = palpitesComPlacar.Count;

            return palpitesComPlacar
                .GroupBy(p => $"{p.GolsCasaPalpite} x {p.GolsVisitantePalpite}")
                .Select(g => new PalpiteComunidadePlacarViewModel
                {
                    Placar = g.Key,
                    Total = g.Count(),
                    Percentual = CalcularPercentual(g.Count(), totalComPlacar)
                })
                .OrderByDescending(p => p.Total)
                .ThenBy(p => p.Placar)
                .Take(3)
                .ToList();
        }

        private static bool PlacarValido(
            int? golsCasa,
            int? golsVisitante,
            out string mensagem)
        {
            mensagem = string.Empty;

            if (!golsCasa.HasValue && !golsVisitante.HasValue)
            {
                return true;
            }

            if (golsCasa.HasValue != golsVisitante.HasValue)
            {
                mensagem = "Preencha os dois placares ou deixe os dois vazios.";
                return false;
            }

            if (golsCasa is < 0 or > 50 || golsVisitante is < 0 or > 50)
            {
                mensagem = "Informe placares entre 0 e 50.";
                return false;
            }

            return true;
        }

        private static string NormalizarResultado(string? resultado)
        {
            return resultado?.Trim() switch
            {
                ResultadoCasa => ResultadoCasa,
                ResultadoEmpate => ResultadoEmpate,
                ResultadoVisitante => ResultadoVisitante,
                _ => string.Empty
            };
        }

        private static int CalcularPercentual(int valor, int total)
        {
            return total == 0
                ? 0
                : (int)Math.Round(valor * 100.0 / total);
        }

        private static bool JogoFinalizado(Jogo jogo)
        {
            return string.Equals(jogo.Status, "Finalizado", StringComparison.OrdinalIgnoreCase) &&
                jogo.GolsCasa.HasValue &&
                jogo.GolsVisitante.HasValue;
        }

        private static string ObterResultado(int golsCasa, int golsVisitante)
        {
            if (golsCasa > golsVisitante)
            {
                return ResultadoCasa;
            }

            if (golsCasa < golsVisitante)
            {
                return ResultadoVisitante;
            }

            return ResultadoEmpate;
        }

        public static string TextoResultado(string resultado)
        {
            return resultado switch
            {
                ResultadoCasa => "Vitória do mandante",
                ResultadoVisitante => "Vitória do visitante",
                ResultadoEmpate => "Empate",
                _ => "Sem palpite"
            };
        }
    }
}
