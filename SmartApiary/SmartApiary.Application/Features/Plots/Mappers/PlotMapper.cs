using SmartApiary.Application.Features.Plots.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Domain.Models;

namespace SmartApiary.Application.Features.Plots.Mappers
{
    internal class PlotMapper : IMapper<Plot, PlotDto>
    {
        public PlotDto Map(Plot source) => new()
        {
            Id = source.Id.Value,
            Name = source.Name,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            CreatedAt = source.CreatedAt,
            Culture = source.Culture is null ? null : new PlotCultureDto(
                Id: source.Culture.Id.Value,
                Type: source.Culture.Type,
                TypeName: source.Culture.Type.ToString(),
                BloomingStart: source.Culture.BloomingStart,
                BloomingEnd: source.Culture.BloomingEnd,
                AdditionalInfo: source.Culture.AdditionalInfo)
        };
    }
}