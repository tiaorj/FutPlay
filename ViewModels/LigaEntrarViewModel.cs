using System.ComponentModel.DataAnnotations;

namespace FutPlay.ViewModels
{
    public class LigaEntrarViewModel
    {
        [Display(Name = "Código de Convite")]
        [Required(ErrorMessage = "Informe o código de convite.")]
        public string? CodigoConvite { get; set; }
    }
}