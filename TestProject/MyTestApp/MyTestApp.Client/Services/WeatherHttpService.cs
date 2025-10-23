using System.Net.Http.Json;
using Microsoft.JSInterop;
using MyTestApp.Shared.Services;

namespace MyTestApp.Client.Services
{
    public class ClientWeatherService : IWeatherService
    {
        private readonly ApiClient _apiClient;

        public ClientWeatherService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<WeatherForecast[]> GetForecastAsync()
        {
            try
            {
                var forecasts = await _apiClient.GetJsonAsync<WeatherForecast[]>("api/weather");
                return forecasts ?? new WeatherForecast[0];
            }
            catch
            {
                return new WeatherForecast[0];
            }
        }
    }
}
