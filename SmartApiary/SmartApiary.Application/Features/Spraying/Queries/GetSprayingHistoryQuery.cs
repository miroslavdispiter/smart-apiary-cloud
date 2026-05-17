using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Spraying.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Enums.Farmer;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Spraying.Queries
{
    // QUERY
    public record GetSprayingHistoryQuery : IRequest<Result<IReadOnlyCollection<SprayingScheduleDto>>>
    {
        public string? PlotId { get; init; }           // optional filter by plot
        public DateTime? From { get; init; }            // optional date range filter
        public DateTime? To { get; init; }
        public bool CompletedOnly { get; init; } = false; // digital card shows only completed
    }

    // VALIDATOR
    public class GetSprayingHistoryValidator : AbstractValidator<GetSprayingHistoryQuery>
    {
        public GetSprayingHistoryValidator()
        {
            When(x => x.From.HasValue && x.To.HasValue, () =>
            {
                RuleFor(x => x.To)
                    .GreaterThan(x => x.From).WithMessage("'To' date must be after 'From' date.");
            });
        }
    }

    // HANDLER
    internal class GetSprayingHistoryHandler(
        IPlotRepository plotRepository,
        ISprayingRepository sprayingRepository,
        ICurrentUserService currentUserService,
        IMapper<SprayingSchedule, SprayingScheduleDto> mapper,
        ILogger<GetSprayingHistoryHandler> logger) : IRequestHandler<GetSprayingHistoryQuery, Result<IReadOnlyCollection<SprayingScheduleDto>>>
    {
        public async Task<Result<IReadOnlyCollection<SprayingScheduleDto>>> Handle(GetSprayingHistoryQuery request, CancellationToken ct)
        {
            var ownerId = currentUserService.UserId;

            try
            {
                // Gather plots to query — either one specific plot or all farmer's plots
                IReadOnlyCollection<Plot> plots;

                if (!string.IsNullOrWhiteSpace(request.PlotId))
                {
                    var idResult = EntityId.Create(request.PlotId);
                    if (idResult.IsFailure)
                        return Result<IReadOnlyCollection<SprayingScheduleDto>>.Failure(idResult.Error!.Message, ErrorType.Validation);

                    var plot = await plotRepository.GetByIdAsync(idResult.Value, ct);
                    if (plot is null)
                        return Result<IReadOnlyCollection<SprayingScheduleDto>>.Failure("Plot not found.", ErrorType.NotFound);

                    if (!plot.IsOwnedBy(ownerId))
                        return Result<IReadOnlyCollection<SprayingScheduleDto>>.Failure("Access denied.", ErrorType.Forbidden);

                    plots = [plot];
                }
                else
                {
                    plots = await plotRepository.GetAllByOwnerAsync(ownerId, ct);
                }

                var result = new List<SprayingScheduleDto>();

                foreach (var plot in plots)
                {
                    var schedules = await sprayingRepository.GetByPlotIdAsync(plot.Id, ct);

                    var filtered = schedules
                        .Where(s => !request.CompletedOnly || s.Status == SprayingStatus.Completed)
                        .Where(s => !request.From.HasValue || s.ScheduledAt >= request.From.Value)
                        .Where(s => !request.To.HasValue || s.ScheduledAt <= request.To.Value)
                        .Select(s =>
                        {
                            var dto = mapper.Map(s);
                            // Inject plot name since mapper doesn't have it
                            return dto with { PlotName = plot.Name };
                        });

                    result.AddRange(filtered);
                }

                var sorted = result
                    .OrderByDescending(s => s.ScheduledAt)
                    .ToList()
                    .AsReadOnly();

                return Result<IReadOnlyCollection<SprayingScheduleDto>>.Success(sorted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SPRAYING] Error fetching spraying history for farmer {OwnerId}.", ownerId);
                return Result<IReadOnlyCollection<SprayingScheduleDto>>.Failure("Failed to fetch spraying history.", ErrorType.Failure);
            }
        }
    }
}
