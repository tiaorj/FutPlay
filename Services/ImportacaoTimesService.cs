using FutPlay.Data;
using FutPlay.Models;
using FutPlay.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FutPlay.Services
{
    public class ImportacaoTimesService
    {
        private readonly FootballApiService _footballApiService;
        private readonly AppDbContext _context;

        public ImportacaoTimesService(
            FootballApiService footballApiService,
            AppDbContext context)
        {
            _footballApiService = footballApiService;
            _context = context;
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesAsync(string pais)
        {
            using var resultado = await _footballApiService.BuscarTimesAsync(pais);

            return await MontarTimesAsync(resultado, pais);
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesPorNomeAsync(string nome)
        {
            using var resultado = await _footballApiService.BuscarTimesPorNomeAsync(nome);

            return await MontarTimesAsync(resultado);
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesPorPaisENomeAsync(string pais, string nome)
        {
            using var resultado = await _footballApiService.BuscarTimesPorPaisENomeAsync(pais, nome);

            return await MontarTimesAsync(resultado, pais);
        }

        public async Task<List<ApiTimeViewModel>> PesquisarTimesAsync(string termo)
        {
            using var resultado = await _footballApiService.PesquisarTimesAsync(termo);

            return await MontarTimesAsync(resultado);
        }

        public async Task<List<ApiTimeViewModel>> BuscarTimesPorLigaAsync(int leagueId, int temporada)
        {
            using var resultado = await _footballApiService.BuscarTimesPorLigaAsync(leagueId, temporada);

            return await MontarTimesAsync(resultado);
        }

        public async Task<ImportacaoTimeResultado> ImportarTimeAsync(
            int apiTeamId,
            string nome,
            string? pais,
            string? escudoUrl,
            string? sigla,
            string? tipo,
            bool? ativo = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return ImportacaoTimeResultado.Falha("Não foi possível importar o time porque o nome veio vazio da API.");
            }

            var timeExistente = await LocalizarTimeExistenteAsync(apiTeamId, nome, pais);

            if (timeExistente != null)
            {
                return ImportacaoTimeResultado.Falha(
                    $"O time {timeExistente.Nome} já está cadastrado. Use Atualizar dados para sincronizar as informações.");
            }

            var time = new Time
            {
                Nome = nome.Trim(),
                Sigla = ObterSiglaValida(sigla, nome),
                Pais = ObterTextoValido(pais),
                Tipo = ObterTipoValido(tipo) ?? "Clube",
                EscudoUrl = ObterTextoValido(escudoUrl),
                Ativo = ativo ?? true,
                ApiTeamId = apiTeamId
            };

            _context.Times.Add(time);
            await _context.SaveChangesAsync();

            return ImportacaoTimeResultado.Importado(time.Nome);
        }

        public async Task<ImportacaoTimeResultado> AtualizarTimeAsync(
            int apiTeamId,
            string nome,
            string? pais,
            string? escudoUrl,
            string? sigla,
            string? tipo,
            bool? ativo = null)
        {
            var time = await LocalizarTimeExistenteAsync(apiTeamId, nome, pais);

            if (time == null)
            {
                return ImportacaoTimeResultado.Falha("Time não encontrado para atualização. Faça a importação primeiro.");
            }

            if (apiTeamId > 0 && time.ApiTeamId != apiTeamId)
            {
                time.ApiTeamId = apiTeamId;
            }

            var nomeValido = ObterTextoValido(nome);
            if (!string.IsNullOrWhiteSpace(nomeValido))
            {
                time.Nome = nomeValido;
            }

            var paisValido = ObterTextoValido(pais);
            if (!string.IsNullOrWhiteSpace(paisValido))
            {
                time.Pais = paisValido;
            }

            var escudoValido = ObterTextoValido(escudoUrl);
            if (!string.IsNullOrWhiteSpace(escudoValido))
            {
                time.EscudoUrl = escudoValido;
            }

            var siglaValida = ObterSiglaApiValida(sigla);
            if (!string.IsNullOrWhiteSpace(siglaValida))
            {
                time.Sigla = siglaValida;
            }

            var tipoValido = ObterTipoValido(tipo);
            if (!string.IsNullOrWhiteSpace(tipoValido))
            {
                time.Tipo = tipoValido;
            }

            if (ativo.HasValue)
            {
                time.Ativo = ativo.Value;
            }

            _context.Times.Update(time);
            await _context.SaveChangesAsync();

            return ImportacaoTimeResultado.Atualizado(time.Nome);
        }

        private async Task<List<ApiTimeViewModel>> MontarTimesAsync(
            JsonDocument resultado,
            string? paisPadrao = null)
        {
            var times = new List<ApiTimeViewModel>();

            if (resultado.RootElement.TryGetProperty("response", out var response))
            {
                foreach (var item in response.EnumerateArray())
                {
                    var team = item.GetProperty("team");

                    int apiTeamId = team.GetProperty("id").GetInt32();
                    string nome = team.GetProperty("name").GetString() ?? string.Empty;

                    string? pais = team.TryGetProperty("country", out var countryElement)
                        ? countryElement.GetString()
                        : paisPadrao;

                    string? logo = team.TryGetProperty("logo", out var logoElement)
                        ? logoElement.GetString()
                        : null;

                    string? codigo = team.TryGetProperty("code", out var codeElement)
                        ? codeElement.GetString()
                        : null;

                    bool? selecao = team.TryGetProperty("national", out var nationalElement) &&
                        nationalElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                        ? nationalElement.GetBoolean()
                        : null;

                    var tipo = selecao == true ? "Seleção" : "Clube";
                    var timeExistente = await LocalizarTimeExistenteAsync(apiTeamId, nome, pais);

                    times.Add(new ApiTimeViewModel
                    {
                        ApiTeamId = apiTeamId,
                        Nome = nome,
                        Pais = pais,
                        CodigoPais = codigo,
                        Sigla = codigo,
                        Tipo = tipo,
                        EscudoUrl = logo,
                        JaImportado = timeExistente != null,
                        TimeExistenteId = timeExistente?.Id
                    });
                }
            }

            return times
                .OrderBy(t => t.Nome)
                .ToList();
        }

        private async Task<Time?> LocalizarTimeExistenteAsync(
            int apiTeamId,
            string? nome,
            string? pais)
        {
            if (apiTeamId > 0)
            {
                var timePorApi = await _context.Times
                    .FirstOrDefaultAsync(t => t.ApiTeamId == apiTeamId);

                if (timePorApi != null)
                {
                    return timePorApi;
                }
            }

            var nomeBusca = ObterTextoValido(nome);
            var paisBusca = ObterTextoValido(pais);

            if (string.IsNullOrWhiteSpace(nomeBusca) || string.IsNullOrWhiteSpace(paisBusca))
            {
                return null;
            }

            return await _context.Times
                .FirstOrDefaultAsync(t => t.Nome == nomeBusca && t.Pais == paisBusca);
        }

        private static string GerarSigla(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return string.Empty;
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

        private static string? ObterTextoValido(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private static string? ObterSiglaValida(string? sigla, string? nome)
        {
            var siglaLimpa = ObterSiglaApiValida(sigla);

            if (!string.IsNullOrWhiteSpace(siglaLimpa))
            {
                return siglaLimpa;
            }

            var nomeLimpo = ObterTextoValido(nome);

            return string.IsNullOrWhiteSpace(nomeLimpo)
                ? null
                : GerarSigla(nomeLimpo);
        }

        private static string? ObterSiglaApiValida(string? sigla)
        {
            var siglaLimpa = ObterTextoValido(sigla);

            if (string.IsNullOrWhiteSpace(siglaLimpa))
            {
                return null;
            }

            return siglaLimpa.Length <= 10
                ? siglaLimpa.ToUpperInvariant()
                : siglaLimpa[..10].ToUpperInvariant();
        }

        private static string? ObterTipoValido(string? tipo)
        {
            if (string.Equals(tipo, "Seleção", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tipo, "Selecao", StringComparison.OrdinalIgnoreCase))
            {
                return "Seleção";
            }

            if (string.Equals(tipo, "Clube", StringComparison.OrdinalIgnoreCase))
            {
                return "Clube";
            }

            return null;
        }
    }

    public class ImportacaoTimeResultado
    {
        public bool Sucesso { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public static ImportacaoTimeResultado Importado(string nome)
        {
            return new ImportacaoTimeResultado
            {
                Sucesso = true,
                Mensagem = $"Time {nome} importado com sucesso."
            };
        }

        public static ImportacaoTimeResultado Atualizado(string nome)
        {
            return new ImportacaoTimeResultado
            {
                Sucesso = true,
                Mensagem = $"Dados do time {nome} atualizados com sucesso."
            };
        }

        public static ImportacaoTimeResultado Falha(string mensagem)
        {
            return new ImportacaoTimeResultado
            {
                Sucesso = false,
                Mensagem = mensagem
            };
        }
    }
}
