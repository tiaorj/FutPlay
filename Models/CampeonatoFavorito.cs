using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FutPlay.Models
{
    public class CampeonatoFavorito
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? Usuario { get; set; }

        [Required]
        public int CampeonatoId { get; set; }

        [ForeignKey("CampeonatoId")]
        public Campeonato? Campeonato { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;
    }
}
