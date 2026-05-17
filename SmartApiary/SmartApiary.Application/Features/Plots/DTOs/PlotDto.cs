using SmartApiary.Domain.Enums.Farmer;

namespace SmartApiary.Application.Features.Plots.DTOs
{
    public record PlotDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public DateTime CreatedAt { get; init; }
        public PlotCultureDto? Culture { get; init; }
    }
}