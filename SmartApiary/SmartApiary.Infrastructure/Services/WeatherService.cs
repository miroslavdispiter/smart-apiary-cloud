using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Common.Options;
using System.Text.Json;
using System.Net.Http;

namespace SmartApiary.Infrastructure.Services
{
    internal class WeatherService(
        IHttpClientFactory httpClientFactory,
        IOptions<WeatherOptions> options,
        ILogger<WeatherService> logger) : IWeatherService
    {
        private readonly WeatherOptions _options = options.Value;

        public async Task<WeatherConditions?> GetForecastAsync(
            double latitude,
            double longitude,
            DateTime atTime,
            CancellationToken ct = default)
        {
            try
            {
                // OpenWeatherMap 5-day/3-hour forecast — free tier
                var url = $"https://api.openweathermap.org/data/2.5/forecast" +
                          $"?lat={latitude}&lon={longitude}" +
                          $"&appid={_options.ApiKey}&units=metric";

                var client = httpClientFactory.CreateClient("weather");
                var response = await client.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("[WEATHER] OpenWeatherMap returned {Status} for ({Lat},{Lng}).",
                        response.StatusCode, latitude, longitude);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                var list = doc.RootElement.GetProperty("list");

                // Find the forecast entry closest to atTime
                ForecastEntry? closest = null;
                var minDiff = TimeSpan.MaxValue;

                foreach (var item in list.EnumerateArray())
                {
                    var dt = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).UtcDateTime;
                    var diff = (dt - atTime).Duration();

                    if (diff >= minDiff) continue;
                    minDiff = diff;

                    var wind = item.GetProperty("wind").GetProperty("speed").GetDouble();
                    var weatherArr = item.GetProperty("weather");
                    var description = weatherArr[0].GetProperty("description").GetString() ?? string.Empty;
                    var mainWeather = weatherArr[0].GetProperty("main").GetString() ?? string.Empty;

                    var isRaining = mainWeather.Equals("Rain", StringComparison.OrdinalIgnoreCase)
                                 || mainWeather.Equals("Drizzle", StringComparison.OrdinalIgnoreCase)
                                 || mainWeather.Equals("Thunderstorm", StringComparison.OrdinalIgnoreCase);

                    closest = new ForecastEntry(wind, isRaining, description);
                }

                if (closest is null) return null;

                return new WeatherConditions(closest.WindSpeedMs, closest.IsRaining, closest.Description);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WEATHER] Error fetching forecast for ({Lat},{Lng}).", latitude, longitude);
                return null; // Non-fatal — weather check is advisory
            }
        }

        private record ForecastEntry(double WindSpeedMs, bool IsRaining, string Description);
    }
}
