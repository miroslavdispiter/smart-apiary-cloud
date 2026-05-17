using SmartApiary.Domain.Enums.Farmer;

namespace SmartApiary.Application.Features.Spraying.DTOs
{
    public record SprayingScheduleDto
    {
        public string Id { get; init; } = string.Empty;
        public string PlotId { get; init; } = string.Empty;
        public string PlotName { get; init; } = string.Empty;
        public DateTime ScheduledAt { get; init; }
        public int DurationHours { get; init; }
        public string? Preparation { get; init; }
        public SprayingStatus Status { get; init; }
        public string StatusName => Status.ToString();
        public int NotifiedBeekeeperCount { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}