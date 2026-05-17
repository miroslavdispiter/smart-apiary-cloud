namespace SmartApiary.Application.Interfaces
{
    public record WeatherConditions(
        double WindSpeedMs,
        bool IsRaining,
        string Description);

    public interface IWeatherService
    {
        Task<WeatherConditions?> GetForecastAsync(
            double latitude,
            double longitude,
            DateTime atTime,
            CancellationToken ct = default);
    }
}
