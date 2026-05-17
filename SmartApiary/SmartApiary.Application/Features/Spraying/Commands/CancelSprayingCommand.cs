using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Spraying.Commands
{
    public record CancelSprayingCommand(string PlotId, string ScheduleId) : IRequest<Result>;

    public class CancelSprayingValidator : AbstractValidator<CancelSprayingCommand>
    {
        public CancelSprayingValidator()
        {
            RuleFor(x => x.PlotId).NotEmpty().WithMessage("Plot ID is required.");
            RuleFor(x => x.ScheduleId).NotEmpty().WithMessage("Schedule ID is required.");
        }
    }

    internal class CancelSprayingHandler(
        IPlotRepository plotRepository,
        ISprayingRepository sprayingRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IEmailService emailService,
        ILogger<CancelSprayingHandler> logger) : IRequestHandler<CancelSprayingCommand, Result>
    {
        private const double NotificationRadiusMeters = 5000;

        public async Task<Result> Handle(CancelSprayingCommand request, CancellationToken ct)
        {
            var now = dateTimeProvider.UtcNow;
            var ownerId = currentUserService.UserId;

            var plotIdResult = EntityId.Create(request.PlotId);
            if (plotIdResult.IsFailure)
                return Result.Failure(plotIdResult.Error!.Message, ErrorType.Validation);

            var plot = await plotRepository.GetByIdWithSprayingsAsync(plotIdResult.Value, ct);
            if (plot is null)
                return Result.Failure("Plot not found.", ErrorType.NotFound);

            if (!plot.IsOwnedBy(ownerId))
                return Result.Failure("Access denied.", ErrorType.Forbidden);

            var cancelResult = plot.CancelSpraying(request.ScheduleId, now);
            if (cancelResult.IsFailure)
                return cancelResult;

            var scheduleIdResult = EntityId.Create(request.ScheduleId);
            if (scheduleIdResult.IsFailure)
                return Result.Failure(scheduleIdResult.Error!.Message, ErrorType.Validation);

            var schedule = plot.SprayingSchedules.FirstOrDefault(s => s.Id.Value == request.ScheduleId);

            try
            {
                await sprayingRepository.UpdateAsync(schedule!, ct);

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
                    emailService.SendSprayingCancellationAsync(email, plot.Name, schedule!.ScheduledAt, ct));

                await Task.WhenAll(notifyTasks);

                logger.LogInformation("[SPRAYING] Spraying '{ScheduleId}' cancelled for plot '{PlotId}', {Count} beekeepers notified.",
                    request.ScheduleId, request.PlotId, beekeeperEmails.Count);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SPRAYING] Error cancelling spraying '{ScheduleId}'.", request.ScheduleId);
                return Result.Failure("Failed to cancel spraying.", ErrorType.Failure);
            }
        }
    }
}