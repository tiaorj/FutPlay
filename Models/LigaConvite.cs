using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FutPlay.Models
{
    public class LigaConvite
    {
        public int Id { get; set; }

        [Required]
        public int LigaId { get; set; }

        [ForeignKey("LigaId")]
        public Liga? Liga { get; set; }

        [Required(ErrorMessage = "Informe o e-mail do convidado.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NomeConvidado { get; set; }

        [Required]
        [StringLength(100)]
        public string TokenConvite { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CodigoConvite { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pendente";

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public DateTime? DataEnvio { get; set; }

        public DateTime? DataAceite { get; set; }

        [StringLength(450)]
        public string? UserIdAceite { get; set; }

        [ForeignKey("UserIdAceite")]
        public IdentityUser? UsuarioAceite { get; set; }

        public bool Ativo { get; set; } = true;
    }
}