using SmartApiary.Domain.Enums.Farmer;

namespace SmartApiary.Application.Features.Plots.DTOs
{
    public record PlotCultureDto(
        string Id,
        CultureType Type,
        string TypeName,
        DateTime BloomingStart,
        DateTime BloomingEnd,
        string? AdditionalInfo);
}