using SmartApiary.Application.Features.Cultures.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Domain.Models;

namespace SmartApiary.Application.Features.Cultures.Mappers
{
    internal class CultureMapper : IMapper<Culture, CultureDto>
    {
        public CultureDto Map(Culture source) => new(
            Id: source.Id.Value,
            PlotId: source.PlotId.Value,
            Type: source.Type,
            TypeName: source.Type.ToString(),
            BloomingStart: source.BloomingStart,
            BloomingEnd: source.BloomingEnd,
            AdditionalInfo: source.AdditionalInfo);
    }
}