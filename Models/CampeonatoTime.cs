using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class CampeonatoTime
    {
        public int Id { get; set; }

        [Required]
        public int CampeonatoId { get; set; }

        [ForeignKey("CampeonatoId")]
        public Campeonato? Campeonato { get; set; }

        [Required]
        public int TimeId { get; set; }

        [ForeignKey("TimeId")]
        public Time? Time { get; set; }

        public int? GrupoId { get; set; }

        [ForeignKey("GrupoId")]
        public Grupo? Grupo { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
