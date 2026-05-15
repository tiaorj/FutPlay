using FutPlay.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FutPlay.Services
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;

        public DatabaseHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bool conectado = await _context.Database.CanConnectAsync(cancellationToken);

                return conectado
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Banco de dados indisponivel.");
            }
            catch
            {
                return HealthCheckResult.Unhealthy("Banco de dados indisponivel.");
            }
        }
    }
}
