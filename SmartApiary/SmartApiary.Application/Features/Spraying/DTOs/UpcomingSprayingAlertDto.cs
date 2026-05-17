using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartApiary.Application.Features.Spraying.DTOs
{
    public record UpcomingSprayingAlertDto(
        string ScheduleId,
        string PlotId,
        string PlotName,
        DateTime ScheduledAt,
        int DurationHours,
        string? Preparation
    );
}