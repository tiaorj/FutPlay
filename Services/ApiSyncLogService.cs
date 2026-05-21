using FutPlay.Data;
using FutPlay.Models;

namespace FutPlay.Services
{
    public class ApiSyncLogService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ApiSyncLogService> _logger;

        public ApiSyncLogService(
            AppDbContext context,
            ILogger<ApiSyncLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RegistrarAsync(ApiSyncLog log)
        {
            try
            {
                log.CriadoEm = log.CriadoEm == default ? DateTime.UtcNow : log.CriadoEm;
                log.DataInicio = log.DataInicio == default ? DateTime.UtcNow : log.DataInicio;
                log.DataFim ??= DateTime.UtcNow;

                _context.ApiSyncLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gravar log de sincronização da API. Tipo: {Tipo}", log.TipoSincronizacao);
            }
        }
    }
}
