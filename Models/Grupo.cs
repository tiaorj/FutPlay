using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutPlay.Models
{
    public class Grupo
    {
        public int Id { get; set; }

        [Required]
        public int CampeonatoId { get; set; }

        [ForeignKey("CampeonatoId")]
        public Campeonato? Campeonato { get; set; }

        [Required]
        [StringLength(50)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<CampeonatoTime> CampeonatoTimes { get; set; } = new List<CampeonatoTime>();
    }
}
