namespace SmartApiary.WebApi.DTOs
{
    public record TelemetryRequest(
        double WeightKg,
        double Humidity,
        double Temperature,
        int BatteryPercentage,
        DateTime RecordedAt);
}
