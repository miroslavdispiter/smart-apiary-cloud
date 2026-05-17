using SmartApiary.Domain.Enums.Farmer;

namespace SmartApiary.WebApi.DTOs
{
    public record UpdatePlotRequest(
        string Name,
        double Latitude,
        double Longitude);

    public record ScheduleSprayingRequest(
        DateTime ScheduledAt,
        int DurationHours,
        string? Preparation);

    public record RescheduleSprayingRequest(
        DateTime NewScheduledAt,
        int NewDurationHours);

    public record SetCultureRequest(
        CultureType Type,
        DateTime BloomingStart,
        DateTime BloomingEnd,
        string? AdditionalInfo);
}
