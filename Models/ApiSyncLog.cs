using System.ComponentModel.DataAnnotations;

namespace FutPlay.Models
{
    public class ApiSyncLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoSincronizacao { get; set; } = string.Empty;

        public int? CampeonatoId { get; set; }

        public int? TimeId { get; set; }

        public int? ApiLeagueId { get; set; }

        public int? ApiTeamId { get; set; }

        public int? ApiFixtureId { get; set; }

        public int? FootballDataCompetitionId { get; set; }

        public int? FootballDataMatchId { get; set; }

        public int? Temporada { get; set; }

        public DateTime DataInicio { get; set; }

        public DateTime? DataFim { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;

        public int TotalProcessados { get; set; }

        public int TotalCriados { get; set; }

        public int TotalAtualizados { get; set; }

        public int TotalIgnorados { get; set; }

        [StringLength(500)]
        public string? Mensagem { get; set; }

        public string? ErroDetalhado { get; set; }

        [StringLength(450)]
        public string? UsuarioId { get; set; }

        [StringLength(256)]
        public string? UsuarioEmail { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public Campeonato? Campeonato { get; set; }

        public Time? Time { get; set; }
    }
}
