using System.ComponentModel.DataAnnotations;
using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class LigaConviteViewModel
    {
        public int LigaId { get; set; }

        public Liga? Liga { get; set; }

        public string LinkConvite { get; set; } = string.Empty;

        public List<LigaConvite> Convites { get; set; } = new();

        [Display(Name = "Nome do convidado")]
        [StringLength(100)]
        public string? NomeConvidado { get; set; }

        [Display(Name = "E-mail do convidado")]
        [Required(ErrorMessage = "Informe o e-mail do convidado.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Enviar por e-mail agora")]
        public bool EnviarEmail { get; set; } = true;
    }
}
