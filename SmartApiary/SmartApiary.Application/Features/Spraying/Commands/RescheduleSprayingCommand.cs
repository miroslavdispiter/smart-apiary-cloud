using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Spraying.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Spraying.Commands
{
    public record RescheduleSprayingCommand : IRequest<Result<SprayingScheduleDto>>
    {
        public string PlotId { get; init; } = string.Empty;
        public string ScheduleId { get; init; } = string.Empty;
        public DateTime NewScheduledAt { get; init; }
        public int NewDurationHours { get; init; }
    }

    public class RescheduleSprayingValidator : AbstractValidator<RescheduleSprayingCommand>
    {
        public RescheduleSprayingValidator()
        {
            RuleFor(x => x.PlotId).NotEmpty().WithMessage("Plot ID is required.");
            RuleFor(x => x.ScheduleId).NotEmpty().WithMessage("Schedule ID is required.");
            RuleFor(x => x.NewScheduledAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("New scheduled date must be in the future.");
            RuleFor(x => x.NewDurationHours)
                .GreaterThan(0).WithMessage("Duration must be at least 1 hour.");
        }
    }

    internal class RescheduleSprayingHandler(
        IPlotRepository plotRepository,
        ISprayingRepository sprayingRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IWeatherService weatherService,
        IEmailService emailService,
        IMapper<SprayingSchedule, SprayingScheduleDto> mapper,
        ILogger<RescheduleSprayingHandler> logger) : IRequestHandler<RescheduleSprayingCommand, Result<SprayingScheduleDto>>
    {
        private const double NotificationRadiusMeters = 5000;
        private const double MaxWindSpeedMs = 5.0;

        public async Task<Result<SprayingScheduleDto>> Handle(RescheduleSprayingCommand request, CancellationToken ct)
        {
            var now = dateTimeProvider.UtcNow;
            var ownerId = currentUserService.UserId;

            var plotIdResult = EntityId.Create(request.PlotId);
            if (plotIdResult.IsFailure)
                return Result<SprayingScheduleDto>.Failure(plotIdResult.Error!.Message, ErrorType.Validation);

            var plot = await plotRepository.GetByIdWithSprayingsAsync(plotIdResult.Value, ct);
            if (plot is null)
                return Result<SprayingScheduleDto>.Failure("Plot not found.", ErrorType.NotFound);

            if (!plot.IsOwnedBy(ownerId))
                return Result<SprayingScheduleDto>.Failure("Access denied.", ErrorType.Forbidden);

            var weather = await weatherService.GetForecastAsync(plot.Latitude, plot.Longitude, request.NewScheduledAt, ct);
            if (weather is not null && (weather.WindSpeedMs > MaxWindSpeedMs || weather.IsRaining))
            {
                return Result<SprayingScheduleDto>.Failure(
                    $"Bad weather conditions — wind {weather.WindSpeedMs:F1} m/s, rain: {weather.IsRaining}. Consider a different time.",
                    ErrorType.Validation);
            }

            var rescheduleResult = plot.RescheduleSpraying(request.ScheduleId, request.NewScheduledAt, request.NewDurationHours, now);
            if (rescheduleResult.IsFailure)
                return Result<SprayingScheduleDto>.Failure(rescheduleResult.Error!.Message, rescheduleResult.Error!.Type);

            var schedule = plot.SprayingSchedules.First(s => s.Id.Value == request.ScheduleId);

            try
            {
                await sprayingRepository.UpdateAsync(schedule, ct);

                var beekeeperIds = await plotRepository.GetBeekeeperIdsNearAsync(
                    plot.Latitude, plot.Longitude, NotificationRadiusMeters, ct);

                var beekeeperEmails = new List<string>();

                if (beekeeperIds.Any())
                {
                    var entityIds = beekeeperIds
                        .Select(id => EntityId.Create(id))
                        .Where(res => res.IsSuccess)
                        .Select(res => res.Value!)
                        .ToList();

                    var users = await userRepository.GetByIdsAsync(entityIds, ct);
                    beekeeperEmails.AddRange(users.Select(u => u.Email));
                }

                var notifyTasks = beekeeperEmails.Select(email =>
                    emailService.SendSprayingRescheduledAsync(email, plot.Name, request.NewScheduledAt, request.NewDurationHours, ct));

                await Task.WhenAll(notifyTasks);

                logger.LogInformation("[SPRAYING] Spraying '{ScheduleId}' rescheduled for plot '{PlotId}', {Count} beekeepers notified.",
                    request.ScheduleId, request.PlotId, beekeeperEmails.Count);

                return Result<SprayingScheduleDto>.Success(mapper.Map(schedule));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SPRAYING] Error rescheduling spraying '{ScheduleId}'.", request.ScheduleId);
                return Result<SprayingScheduleDto>.Failure("Failed to reschedule spraying.", ErrorType.Failure);
            }
        }
    }
}