using Sivar.Os.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Sivar.Os.Services
{
    public class ServerWeatherService : IWeatherService
    {
        private readonly ILogger<ServerWeatherService> _logger;

        public ServerWeatherService(ILogger<ServerWeatherService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<WeatherForecast[]> GetForecastAsync()
        {
            var requestId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("[ServerWeatherService.GetForecastAsync] START - RequestId={RequestId}, Timestamp={Timestamp}",
                requestId, startTime);

            try
            {
                // In a real app, this would query a database
                // For now, we'll return sample data
                var startDate = DateOnly.FromDateTime(DateTime.Now);
                var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
                
                _logger.LogDebug("[ServerWeatherService.GetForecastAsync] Generating forecasts - RequestId={RequestId}, StartDate={StartDate}, SummaryCount={SummaryCount}",
                    requestId, startDate, summaries.Length);

                var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = startDate.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                }).ToArray();

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[ServerWeatherService.GetForecastAsync] SUCCESS - RequestId={RequestId}, ForecastCount={ForecastCount}, Duration={Duration}ms",
                    requestId, forecasts.Length, elapsed);

                return Task.FromResult(forecasts);
            }
            catch (Exception ex)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "[ServerWeatherService.GetForecastAsync] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                    requestId, ex.GetType().Name, elapsed);
                throw;
            }
        }
    }
}
