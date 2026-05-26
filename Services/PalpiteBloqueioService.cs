using FutPlay.Models;

namespace FutPlay.Services
{
    public class PalpiteBloqueioService
    {
        public const int MinutosBloqueioPalpite = 5;

        private readonly AppTimeService _appTimeService;

        public PalpiteBloqueioService(AppTimeService appTimeService)
        {
            _appTimeService = appTimeService;
        }

        public DateTime ObterDataBloqueio(Jogo jogo)
        {
            var dataJogo = _appTimeService.NormalizarHorarioAplicacao(jogo.DataJogo);
            return dataJogo.AddMinutes(-MinutosBloqueioPalpite);
        }

        public bool PalpiteBloqueado(Jogo jogo)
        {
            return PalpiteBloqueado(jogo, _appTimeService.Agora);
        }

        public bool PalpiteBloqueado(Jogo jogo, DateTime agoraAplicacao)
        {
            return agoraAplicacao >= ObterDataBloqueio(jogo);
        }
    }
}
