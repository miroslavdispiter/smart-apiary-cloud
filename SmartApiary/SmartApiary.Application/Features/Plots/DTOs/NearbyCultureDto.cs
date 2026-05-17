using SmartApiary.Domain.Enums.Farmer;

namespace SmartApiary.Application.Features.Plots.DTOs
{
    public record NearbyCultureDto(
        string PlotId,
        string PlotName,
        double Latitude,
        double Longitude,
        CultureType CultureType,
        DateTime BloomingStart,
        DateTime BloomingEnd,
        string? AdditionalInfo
    );
}