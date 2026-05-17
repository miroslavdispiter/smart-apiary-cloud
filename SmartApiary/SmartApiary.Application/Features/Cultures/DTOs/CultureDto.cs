using SmartApiary.Domain.Enums.Farmer;

namespace SmartApiary.Application.Features.Cultures.DTOs
{
    public record CultureDto(
        string Id,
        string PlotId,
        CultureType Type,
        string TypeName,
        DateTime BloomingStart,
        DateTime BloomingEnd,
        string? AdditionalInfo);
}