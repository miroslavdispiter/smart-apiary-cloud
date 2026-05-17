using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Plots.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Plots.Queries.GetNearbyCultures
{
    internal class GetNearbyCulturesHandler(
        IApiaryRepository apiaryRepository,
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        ILogger<GetNearbyCulturesHandler> logger)
        : IRequestHandler<GetNearbyCulturesQuery, Result<IReadOnlyCollection<NearbyCultureDto>>>
    {
        private const double NotificationRadiusMeters = 5000;

        public async Task<Result<IReadOnlyCollection<NearbyCultureDto>>> Handle(
            GetNearbyCulturesQuery request,
            CancellationToken ct)
        {
            var ownerId = currentUserService.UserId;

            var idResult = EntityId.Create(request.ApiaryId);
            if (idResult.IsFailure)
                return Result<IReadOnlyCollection<NearbyCultureDto>>
                    .Failure(idResult.Error!.Message, ErrorType.Validation);

            var apiary = await apiaryRepository.GetByIdAsync(idResult.Value, ct);
            if (apiary is null)
                return Result<IReadOnlyCollection<NearbyCultureDto>>
                    .Failure("Apiary not found.", ErrorType.NotFound);

            if (!apiary.IsOwnedBy(ownerId))
                return Result<IReadOnlyCollection<NearbyCultureDto>>
                    .Failure("Access denied.", ErrorType.Unauthorized);

            try
            {
                var plots = await plotRepository.GetPlotsWithCultureNearAsync(
                    apiary.Latitude,
                    apiary.Longitude,
                    NotificationRadiusMeters,
                    ct);

                var dtos = plots
                    .Where(p => p.Culture is not null)
                    .Select(p => new NearbyCultureDto(
                        p.Id.Value,
                        p.Name,
                        p.Latitude,
                        p.Longitude,
                        p.Culture!.Type,
                        p.Culture.BloomingStart,
                        p.Culture.BloomingEnd,
                        p.Culture.AdditionalInfo))
                    .ToList()
                    .AsReadOnly();

                return Result<IReadOnlyCollection<NearbyCultureDto>>
                    .Success(dtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[CULTURE] Error fetching nearby cultures for apiary {ApiaryId}.",
                    request.ApiaryId);

                return Result<IReadOnlyCollection<NearbyCultureDto>>
                    .Failure("Failed to fetch nearby cultures.", ErrorType.Failure);
            }
        }
    }
}