using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Spraying.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Enums.Farmer;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Spraying.Queries.GetUpcomingSprayingsNearApiary
{
    internal class GetUpcomingSprayingsNearApiaryHandler(
        IApiaryRepository apiaryRepository,
        ISprayingRepository sprayingRepository,
        ICurrentUserService currentUserService,
        ILogger<GetUpcomingSprayingsNearApiaryHandler> logger)
        : IRequestHandler<GetUpcomingSprayingsNearApiaryQuery, Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>>
    {
        private const double NotificationRadiusMeters = 5000;

        public async Task<Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>> Handle(
            GetUpcomingSprayingsNearApiaryQuery request,
            CancellationToken ct)
        {
            var ownerId = currentUserService.UserId;

            var idResult = EntityId.Create(request.ApiaryId);
            if (idResult.IsFailure)
                return Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>
                    .Failure(idResult.Error!.Message, ErrorType.Validation);

            var apiary = await apiaryRepository.GetByIdAsync(idResult.Value, ct);
            if (apiary is null)
                return Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>
                    .Failure("Apiary not found.", ErrorType.NotFound);

            if (!apiary.IsOwnedBy(ownerId))
                return Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>
                    .Failure("Access denied.", ErrorType.Unauthorized);

            try
            {
                var upcomingSprayings = await sprayingRepository
                    .GetUpcomingNearLocationAsync(
                        apiary.Latitude,
                        apiary.Longitude,
                        NotificationRadiusMeters,
                        ct);

                var dtos = upcomingSprayings
                    .Where(x => x.Schedule.Status == SprayingStatus.Scheduled)
                    .OrderBy(x => x.Schedule.ScheduledAt)
                    .Select(x => new UpcomingSprayingAlertDto(
                        x.Schedule.Id.Value,
                        x.Schedule.PlotId.Value,
                        x.PlotName,
                        x.Schedule.ScheduledAt,
                        x.Schedule.DurationHours,
                        x.Schedule.Preparation))
                    .ToList()
                    .AsReadOnly();

                return Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>
                    .Success(dtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[SPRAYING] Error fetching upcoming sprayings near apiary {ApiaryId}.",
                    request.ApiaryId);

                return Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>
                    .Failure("Failed to fetch upcoming sprayings.", ErrorType.Failure);
            }
        }
    }
}