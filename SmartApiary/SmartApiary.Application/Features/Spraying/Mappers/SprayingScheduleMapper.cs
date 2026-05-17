using SmartApiary.Application.Features.Spraying.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Domain.Models;

namespace SmartApiary.Application.Features.Spraying.Mappers
{
    internal class SprayingScheduleMapper : IMapper<SprayingSchedule, SprayingScheduleDto>
    {
        public SprayingScheduleDto Map(SprayingSchedule source)
        {
            return new SprayingScheduleDto
            {
                Id = source.Id.Value,
                PlotId = source.PlotId.Value,
                PlotName = string.Empty, // populated in handler when needed
                ScheduledAt = source.ScheduledAt,
                DurationHours = source.DurationHours,
                Preparation = source.Preparation,
                Status = source.Status,
                NotifiedBeekeeperCount = source.NotifiedBeekeeperCount,
                CreatedAt = source.CreatedAt
            };
        }
    }
}