namespace FutPlay.Services
{
    internal static class FootballApiStatusMapper
    {
        public static string ConverterStatusJogo(string statusApi)
        {
            return statusApi switch
            {
                "NS" => "Agendado",
                "TBD" => "Agendado",
                "1H" => "Em andamento",
                "HT" => "Em andamento",
                "2H" => "Em andamento",
                "ET" => "Em andamento",
                "BT" => "Em andamento",
                "P" => "Em andamento",
                "SUSP" => "Cancelado",
                "INT" => "Cancelado",
                "FT" => "Finalizado",
                "AET" => "Finalizado",
                "PEN" => "Finalizado",
                "PST" => "Cancelado",
                "CANC" => "Cancelado",
                "ABD" => "Cancelado",
                "AWD" => "Finalizado",
                "WO" => "Finalizado",
                _ => "Agendado"
            };
        }
    }
}
