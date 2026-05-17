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
    public record ScheduleSprayingCommand : IRequest<Result<SprayingScheduleDto>>
    {
        public string PlotId { get; init; } = string.Empty;
        public DateTime ScheduledAt { get; init; }
        public int DurationHours { get; init; }
        public string? Preparation { get; init; }
    }

    public class ScheduleSprayingValidator : AbstractValidator<ScheduleSprayingCommand>
    {
        public ScheduleSprayingValidator()
        {
            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("Plot ID is required.");

            RuleFor(x => x.ScheduledAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date must be in the future.");

            RuleFor(x => x.DurationHours)
                .GreaterThan(0).WithMessage("Duration must be at least 1 hour.");

            RuleFor(x => x.Preparation)
                .MaximumLength(200).WithMessage("Preparation name must not exceed 200 characters.")
                .When(x => x.Preparation is not null);
        }
    }

    internal class ScheduleSprayingHandler(
        IPlotRepository plotRepository,
        ISprayingRepository sprayingRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IWeatherService weatherService,
        IEmailService emailService,
        IMapper<SprayingSchedule, SprayingScheduleDto> mapper,
        ILogger<ScheduleSprayingHandler> logger) : IRequestHandler<ScheduleSprayingCommand, Result<SprayingScheduleDto>>
    {
        private const double NotificationRadiusMeters = 5000;
        private const double MaxWindSpeedMs = 5.0;

        public async Task<Result<SprayingScheduleDto>> Handle(ScheduleSprayingCommand request, CancellationToken ct)
        {
            var now = dateTimeProvider.UtcNow;
            var ownerId = currentUserService.UserId;

            var idResult = EntityId.Create(request.PlotId);
            if (idResult.IsFailure)
                return Result<SprayingScheduleDto>.Failure(idResult.Error!.Message, ErrorType.Validation);

            var plot = await plotRepository.GetByIdAsync(idResult.Value, ct);
            if (plot is null)
                return Result<SprayingScheduleDto>.Failure("Plot not found.", ErrorType.NotFound);

            if (!plot.IsOwnedBy(ownerId))
                return Result<SprayingScheduleDto>.Failure("Access denied.", ErrorType.Forbidden);

            var weather = await weatherService.GetForecastAsync(plot.Latitude, plot.Longitude, request.ScheduledAt, ct);
            if (weather is not null && (weather.WindSpeedMs > MaxWindSpeedMs || weather.IsRaining))
            {
                logger.LogWarning("[SPRAYING] Bad weather conditions (wind: {Wind} m/s, rain: {Rain}) for plot '{PlotId}' at {Time}.",
                    weather.WindSpeedMs, weather.IsRaining, plot.Id.Value, request.ScheduledAt);

                return Result<SprayingScheduleDto>.Failure(
                    $"Bad weather conditions — wind {weather.WindSpeedMs:F1} m/s, rain: {weather.IsRaining}. Consider rescheduling.",
                    ErrorType.Validation);
            }

            var scheduleResult = plot.ScheduleSpraying(request.ScheduledAt, request.DurationHours, request.Preparation, now);
            if (scheduleResult.IsFailure)
                return Result<SprayingScheduleDto>.Failure(scheduleResult.Error!.Message, ErrorType.Validation);

            var schedule = scheduleResult.Value;

            try
            {
                await sprayingRepository.AddAsync(schedule, ct);

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
                    emailService.SendSprayingAnnouncementAsync(
                        email,
                        plot.Name,
                        request.ScheduledAt,
                        request.DurationHours,
                        request.Preparation,
                        ct));

                await Task.WhenAll(notifyTasks);

                schedule.SetNotifiedBeekeeperCount(beekeeperEmails.Count);
                await sprayingRepository.UpdateAsync(schedule, ct);

                logger.LogInformation("[SPRAYING] Spraying scheduled for plot '{PlotId}', {Count} beekeepers notified.",
                    plot.Id.Value, beekeeperEmails.Count);

                return Result<SprayingScheduleDto>.Success(mapper.Map(schedule));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SPRAYING] Error scheduling spraying for plot '{PlotId}'.", request.PlotId);
                return Result<SprayingScheduleDto>.Failure("Failed to schedule spraying.", ErrorType.Failure);
            }
        }
    }
}