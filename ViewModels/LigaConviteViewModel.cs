using FutPlay.Models;

namespace FutPlay.ViewModels
{
    public class LigaConviteViewModel
    {
        public Liga Liga { get; set; } = new();

        public string LinkConvite { get; set; } = string.Empty;
    }
}