using System;
using System.Collections.Generic;

namespace FutPlay.Models
{
    public static class PaisExibicao
    {
        private static readonly Dictionary<string, string> Nomes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Brazil"] = "Brasil",
            ["World"] = "Mundial",
            ["England"] = "Inglaterra",
            ["Spain"] = "Espanha",
            ["Germany"] = "Alemanha",
            ["France"] = "França",
            ["Italy"] = "Itália"
        };

        public static string Normalizar(string? pais)
        {
            if (string.IsNullOrWhiteSpace(pais))
            {
                return "Mundial";
            }

            var nome = pais.Trim();

            return Nomes.TryGetValue(nome, out var normalizado)
                ? normalizado
                : nome;
        }

        public static bool Equivale(string? paisBanco, string? paisFiltro)
        {
            if (string.IsNullOrWhiteSpace(paisFiltro))
            {
                return true;
            }

            return string.Equals(
                Normalizar(paisBanco),
                Normalizar(paisFiltro),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
