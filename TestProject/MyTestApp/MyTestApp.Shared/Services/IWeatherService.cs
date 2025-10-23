namespace MyTestApp.Shared.Services
{
    public interface IWeatherService
    {
        Task<WeatherForecast[]> GetForecastAsync();
    }

    public class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string? Summary { get; set; }
    }
}
