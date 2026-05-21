using FutPlay.Models;

namespace FutPlay.Services
{
    public static class CampeonatoApiFormatoService
    {
        public static string InferirFormato(string? nome, string? tipo, IEnumerable<string?>? fases = null)
        {
            if (fases?.Any(EhFaseDeGrupos) == true)
            {
                return CampeonatoFormato.GruposEMataMata;
            }

            if (EhTipoCopa(tipo) || NomeIndicaCopa(nome))
            {
                return CampeonatoFormato.GruposEMataMata;
            }

            return CampeonatoFormato.PontosCorridos;
        }

        private static bool EhTipoCopa(string? tipo)
        {
            return string.Equals(tipo, "Cup", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tipo, "Copa", StringComparison.OrdinalIgnoreCase);
        }

        private static bool NomeIndicaCopa(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return false;
            }

            var texto = nome.Trim();

            return texto.Contains("copa", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("cup", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("libertadores", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("sul-americana", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("sudamericana", StringComparison.OrdinalIgnoreCase) ||
                   texto.Contains("champions", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EhFaseDeGrupos(string? fase)
        {
            if (string.IsNullOrWhiteSpace(fase))
            {
                return false;
            }

            return fase.Contains("Group", StringComparison.OrdinalIgnoreCase) ||
                   fase.Contains("Grupo", StringComparison.OrdinalIgnoreCase);
        }
    }
}
