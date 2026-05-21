using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FutPlay.Services
{
    public class ImportacaoCampeonatoService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;
        private readonly ApiSyncLogService _apiSyncLogService;
        private readonly ILogger<ImportacaoCampeonatoService> _logger;

        public ImportacaoCampeonatoService(
            FootballApiService footballApiService,
            AppDbContext context,
            ApiSyncLogService apiSyncLogService,
            ILogger<ImportacaoCampeonatoService> logger)
        {
            _footballApiService = footballApiService;
            _context = context;
            _apiSyncLogService = apiSyncLogService;
            _logger = logger;
        }

        public async Task<List<ApiLigaViewModel>> BuscarLigasAsync(string pais, int temporada)
        {
            var ligas = new List<ApiLigaViewModel>();

            using var resultado = await _footballApiService.BuscarLigasAsync(pais, temporada);

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

                    var campeonatoExistente = await LocalizarCampeonatoExistenteAsync(
                        apiLeagueId,
                        nome,
                        paisNome,
                        temporada);

                    ligas.Add(new ApiLigaViewModel
                    {
                        ApiLeagueId = apiLeagueId,
                        Nome = nome,
                        Tipo = tipo,
                        Pais = paisNome,
                        Temporada = temporada,
                        LogoUrl = logo,
                        JaImportado = campeonatoExistente != null,
                        CampeonatoExistenteId = campeonatoExistente?.Id
                    });
                }
            }

            return ligas;
        }

        public async Task<ImportacaoCampeonatoResultado> ImportarLigaAsync(
            int apiLeagueId,
            string nome,
            string tipo,
            string pais,
            string? logoUrl,
            int temporada,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            var inicio = DateTime.UtcNow;
            var campeonatoExistente = await LocalizarCampeonatoExistenteAsync(apiLeagueId, nome, pais, temporada);

            if (campeonatoExistente != null)
            {
                return ImportacaoCampeonatoResultado.Falha(
                    "Este campeonato já foi importado. Use Atualizar dados para sincronizar as informações.",
                    campeonatoExistente.Id);
            }

            var campeonato = new Campeonato
            {
                Nome = nome,
                Ano = temporada,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "Liga" : tipo,
                Formato = CampeonatoApiFormatoService.InferirFormato(nome, tipo),
                Pais = pais,
                LogoUrl = logoUrl,
                Ativo = true,
                ApiLeagueId = apiLeagueId
            };

            _context.Campeonatos.Add(campeonato);
            await _context.SaveChangesAsync();

            await AtualizarMetadadosDaCompeticaoAsync(campeonato);

            await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
            {
                TipoSincronizacao = "Campeonato",
                CampeonatoId = campeonato.Id,
                ApiLeagueId = apiLeagueId,
                Temporada = temporada,
                DataInicio = inicio,
                Status = "Sucesso",
                TotalProcessados = 1,
                TotalCriados = 1,
                Mensagem = $"Campeonato {campeonato.Nome} importado com sucesso.",
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail
            });

            return ImportacaoCampeonatoResultado.Importado(campeonato);
        }

        public async Task<ImportacaoCampeonatoResultado> AtualizarLigaAsync(
            int apiLeagueId,
            string nome,
            string tipo,
            string pais,
            string? logoUrl,
            int temporada,
            bool? ativo = null,
            string? usuarioId = null,
            string? usuarioEmail = null)
        {
            var inicio = DateTime.UtcNow;
            var campeonato = await LocalizarCampeonatoExistenteAsync(apiLeagueId, nome, pais, temporada);

            if (campeonato == null)
            {
                var mensagemErro = "Campeonato não encontrado para atualização. Faça a importação primeiro.";

                await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
                {
                    TipoSincronizacao = "Campeonato",
                    ApiLeagueId = apiLeagueId > 0 ? apiLeagueId : null,
                    Temporada = temporada > 0 ? temporada : null,
                    DataInicio = inicio,
                    Status = "Erro",
                    TotalProcessados = 1,
                    TotalIgnorados = 1,
                    Mensagem = mensagemErro,
                    UsuarioId = usuarioId,
                    UsuarioEmail = usuarioEmail
                });

                return ImportacaoCampeonatoResultado.Falha(mensagemErro);
            }

            if (apiLeagueId > 0 && campeonato.ApiLeagueId != apiLeagueId)
            {
                campeonato.ApiLeagueId = apiLeagueId;
            }

            var nomeValido = ObterTextoValido(nome);
            if (!string.IsNullOrWhiteSpace(nomeValido))
            {
                campeonato.Nome = nomeValido;
            }

            var paisValido = ObterTextoValido(pais);
            if (!string.IsNullOrWhiteSpace(paisValido))
            {
                campeonato.Pais = paisValido;
            }

            var tipoValido = ObterTextoValido(tipo);
            if (!string.IsNullOrWhiteSpace(tipoValido))
            {
                campeonato.Tipo = tipoValido;
            }

            campeonato.Formato = CampeonatoApiFormatoService.InferirFormato(
                nomeValido ?? campeonato.Nome,
                tipoValido ?? campeonato.Tipo);

            var logoValido = ObterTextoValido(logoUrl);
            if (!string.IsNullOrWhiteSpace(logoValido))
            {
                campeonato.LogoUrl = logoValido;
            }

            if (temporada > 0)
            {
                campeonato.Ano = temporada;
            }

            if (ativo.HasValue)
            {
                campeonato.Ativo = ativo.Value;
            }

            _context.Campeonatos.Update(campeonato);
            await _context.SaveChangesAsync();
            await AtualizarMetadadosDaCompeticaoAsync(campeonato);

            await _apiSyncLogService.RegistrarAsync(new ApiSyncLog
            {
                TipoSincronizacao = "Campeonato",
                CampeonatoId = campeonato.Id,
                ApiLeagueId = campeonato.ApiLeagueId,
                Temporada = campeonato.Ano,
                DataInicio = inicio,
                Status = "Sucesso",
                TotalProcessados = 1,
                TotalAtualizados = 1,
                Mensagem = "Dados do campeonato atualizados com sucesso.",
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail
            });

            return ImportacaoCampeonatoResultado.Atualizado(campeonato);
        }

        private async Task<Campeonato?> LocalizarCampeonatoExistenteAsync(
            int apiLeagueId,
            string? nome,
            string? pais,
            int temporada)
        {
            if (apiLeagueId > 0)
            {
                var porApiAno = await _context.Campeonatos
                    .FirstOrDefaultAsync(c => c.ApiLeagueId == apiLeagueId && c.Ano == temporada);

                if (porApiAno != null)
                {
                    return porApiAno;
                }

                var porApi = await _context.Campeonatos
                    .FirstOrDefaultAsync(c => c.ApiLeagueId == apiLeagueId);

                if (porApi != null)
                {
                    return porApi;
                }
            }

            var nomeBusca = ObterTextoValido(nome);
            var paisBusca = ObterTextoValido(pais);

            if (string.IsNullOrWhiteSpace(nomeBusca) || string.IsNullOrWhiteSpace(paisBusca) || temporada <= 0)
            {
                return null;
            }

            return await _context.Campeonatos
                .FirstOrDefaultAsync(c => c.Nome == nomeBusca && c.Pais == paisBusca && c.Ano == temporada);
        }

        private static string? ObterTextoValido(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private async Task AtualizarMetadadosDaCompeticaoAsync(Campeonato campeonato)
        {
            if (!campeonato.ApiLeagueId.HasValue || campeonato.Ano <= 0)
            {
                return;
            }

            try
            {
                using var resultado = await _footballApiService.BuscarJogosAsync(campeonato.ApiLeagueId.Value, campeonato.Ano);
                var fases = ExtrairFases(resultado).ToList();

                campeonato.Formato = CampeonatoApiFormatoService.InferirFormato(
                    campeonato.Nome,
                    campeonato.Tipo,
                    fases);

                await CriarGruposDaCompeticaoAsync(campeonato, fases);
                _context.Campeonatos.Update(campeonato);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Não foi possível atualizar metadados da competição pela API. CampeonatoId: {CampeonatoId}",
                    campeonato.Id);
            }
        }

        private async Task CriarGruposDaCompeticaoAsync(Campeonato campeonato, List<string> fases)
        {
            var gruposApi = fases
                .Select(ExtrairGrupo)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!gruposApi.Any())
            {
                return;
            }

            var gruposExistentes = await _context.Grupos
                .Where(g => g.CampeonatoId == campeonato.Id)
                .ToListAsync();

            foreach (var grupoNome in gruposApi)
            {
                var jaExiste = gruposExistentes.Any(g =>
                    string.Equals(NormalizarGrupo(g.Nome), NormalizarGrupo(grupoNome), StringComparison.OrdinalIgnoreCase));

                if (jaExiste)
                {
                    continue;
                }

                _context.Grupos.Add(new Grupo
                {
                    CampeonatoId = campeonato.Id,
                    Nome = NormalizarGrupo(grupoNome),
                    Ativo = true
                });
            }
        }

        private static IEnumerable<string> ExtrairFases(JsonDocument resultado)
        {
            if (!resultado.RootElement.TryGetProperty("response", out var response))
            {
                yield break;
            }

            foreach (var item in response.EnumerateArray())
            {
                if (!item.TryGetProperty("league", out var league) ||
                    !league.TryGetProperty("round", out var roundElement) ||
                    roundElement.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                var fase = roundElement.GetString();

                if (!string.IsNullOrWhiteSpace(fase))
                {
                    yield return fase;
                }
            }
        }

        private static string? ExtrairGrupo(string fase)
        {
            var match = Regex.Match(fase, @"(?:Group|Grupo)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);

            return match.Success
                ? NormalizarGrupo(match.Groups[1].Value)
                : null;
        }

        private static string NormalizarGrupo(string grupo)
        {
            var texto = grupo.Trim();

            if (texto.StartsWith("Grupo ", StringComparison.OrdinalIgnoreCase))
            {
                texto = texto.Substring("Grupo ".Length).Trim();
            }

            if (texto.StartsWith("Group ", StringComparison.OrdinalIgnoreCase))
            {
                texto = texto.Substring("Group ".Length).Trim();
            }

            return texto.ToUpperInvariant();
        }
    }

    public class ImportacaoCampeonatoResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public int? CampeonatoId { get; set; }

        public static ImportacaoCampeonatoResultado Importado(Campeonato campeonato)
        {
            return new ImportacaoCampeonatoResultado
            {
                Sucesso = true,
                CampeonatoId = campeonato.Id,
                Mensagem = $"Campeonato {campeonato.Nome} importado com sucesso."
            };
        }

        public static ImportacaoCampeonatoResultado Atualizado(Campeonato campeonato)
        {
            return new ImportacaoCampeonatoResultado
            {
                Sucesso = true,
                CampeonatoId = campeonato.Id,
                Mensagem = "Dados do campeonato atualizados com sucesso."
            };
        }

        public static ImportacaoCampeonatoResultado Falha(string mensagem, int? campeonatoId = null)
        {
            return new ImportacaoCampeonatoResultado
            {
                Sucesso = false,
                CampeonatoId = campeonatoId,
                Mensagem = mensagem
            };
        }
    }
}
