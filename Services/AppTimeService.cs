using FutPlay.Settings;
using Microsoft.Extensions.Options;

namespace FutPlay.Services
{
    public class AppTimeService
    {
        private readonly TimeZoneInfo _timeZone;

        public AppTimeService(IOptions<AppTimeOptions> options)
        {
            _timeZone = ResolverTimeZone(options.Value.TimeZoneId);
        }

        public DateTime Agora => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone).DateTime;

        public string TimeZoneId => _timeZone.Id;

        public DateTime ConverterUtcParaHorarioAplicacao(DateTime dataUtc)
        {
            var utc = dataUtc.Kind == DateTimeKind.Utc
                ? new DateTimeOffset(dataUtc)
                : new DateTimeOffset(DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc));

            return TimeZoneInfo.ConvertTime(utc, _timeZone).DateTime;
        }

        public DateTime NormalizarHorarioAplicacao(DateTime data)
        {
            if (data.Kind == DateTimeKind.Utc)
            {
                return ConverterUtcParaHorarioAplicacao(data);
            }

            if (data.Kind == DateTimeKind.Local)
            {
                return TimeZoneInfo.ConvertTime(new DateTimeOffset(data), _timeZone).DateTime;
            }

            return data;
        }

        private static TimeZoneInfo ResolverTimeZone(string? timeZoneId)
        {
            var id = string.IsNullOrWhiteSpace(timeZoneId)
                ? "America/Sao_Paulo"
                : timeZoneId.Trim();

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(MapearTimeZone(id));
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(MapearTimeZone(id));
            }
        }

        private static string MapearTimeZone(string timeZoneId)
        {
            return timeZoneId switch
            {
                "America/Sao_Paulo" => "E. South America Standard Time",
                "E. South America Standard Time" => "America/Sao_Paulo",
                _ => "America/Sao_Paulo"
            };
        }
    }
}
