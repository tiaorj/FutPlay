namespace FutPlay.Models
{
    public static class CampeonatoFormato
    {
        public const string PontosCorridos = "PontosCorridos";
        public const string GruposEMataMata = "Grupos";

        public static bool EhValido(string? formato)
        {
            return string.Equals(formato, PontosCorridos, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(formato, GruposEMataMata, StringComparison.OrdinalIgnoreCase);
        }

        public static bool UsaGrupos(string? formato)
        {
            return string.Equals(formato, GruposEMataMata, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalizar(string? formato)
        {
            return string.Equals(formato, GruposEMataMata, StringComparison.OrdinalIgnoreCase)
                ? GruposEMataMata
                : PontosCorridos;
        }

        public static string ObterDescricao(string? formato)
        {
            return UsaGrupos(formato)
                ? "Grupos + mata-mata"
                : "Pontos corridos";
        }
    }
}
