using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;

namespace SmartApiary.Application.Features.Spraying.Commands.CompleteExpiredSprayings
{
    internal class CompleteExpiredSprayingsHandler(
        ISprayingRepository sprayingRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CompleteExpiredSprayingsHandler> logger)
        : IRequestHandler<CompleteExpiredSprayingsCommand, Result<int>>
    {
        public async Task<Result<int>> Handle(
            CompleteExpiredSprayingsCommand request,
            CancellationToken ct)
        {
            try
            {
                var now = dateTimeProvider.UtcNow;

                var expiredSchedules = await sprayingRepository
                    .GetExpiredScheduledAsync(now, ct);

                var completedCount = 0;

                foreach (var schedule in expiredSchedules)
                {
                    var completeResult = schedule.Complete();

                    if (completeResult.IsFailure)
                    {
                        logger.LogWarning(
                            "[SPRAYING] Could not complete schedule {Id}: {Error}",
                            schedule.Id.Value,
                            completeResult.Error?.Message);
                        continue;
                    }

                    await sprayingRepository.UpdateAsync(schedule, ct);
                    completedCount++;

                    logger.LogInformation(
                        "[SPRAYING] Schedule {Id} automatically completed.",
                        schedule.Id.Value);
                }

                logger.LogInformation(
                    "[SPRAYING] Timer completed {Count} expired sprayings.",
                    completedCount);

                return Result<int>.Success(completedCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SPRAYING] Error completing expired sprayings.");
                return Result<int>.Failure(
                    "Failed to complete expired sprayings.",
                    ErrorType.Failure);
            }
        }
    }
}