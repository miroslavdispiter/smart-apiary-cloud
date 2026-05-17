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
    public record ExportSprayingHistoryQuery : IRequest<Result<byte[]>>
    {
        public string? PlotId { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public bool CompletedOnly { get; init; } = false;
    }

    // VALIDATOR
    public class ExportSprayingHistoryValidator : AbstractValidator<ExportSprayingHistoryQuery>
    {
        public ExportSprayingHistoryValidator()
        {
            When(x => x.From.HasValue && x.To.HasValue, () =>
            {
                RuleFor(x => x.To)
                    .GreaterThan(x => x.From)
                    .WithMessage("'To' date must be after 'From' date.");
            });
        }
    }

    // HANDLER
    internal class ExportSprayingHistoryHandler(
        IPlotRepository plotRepository,
        ISprayingRepository sprayingRepository,
        ICurrentUserService currentUserService,
        ISprayingPdfService pdfService,
        IMapper<SprayingSchedule, SprayingScheduleDto> mapper,
        ILogger<ExportSprayingHistoryHandler> logger) : IRequestHandler<ExportSprayingHistoryQuery, Result<byte[]>>
    {
        public async Task<Result<byte[]>> Handle(ExportSprayingHistoryQuery request, CancellationToken ct)
        {
            var ownerId = currentUserService.UserId;

            try
            {
                IReadOnlyCollection<Plot> plots;
                string? plotName = null;

                if (!string.IsNullOrWhiteSpace(request.PlotId))
                {
                    var idResult = EntityId.Create(request.PlotId);
                    if (idResult.IsFailure)
                        return Result<byte[]>.Failure(idResult.Error!.Message, ErrorType.Validation);

                    var plot = await plotRepository.GetByIdAsync(idResult.Value, ct);
                    if (plot is null)
                        return Result<byte[]>.Failure("Plot not found.", ErrorType.NotFound);

                    if (!plot.IsOwnedBy(ownerId))
                        return Result<byte[]>.Failure("Access denied.", ErrorType.Forbidden);

                    plots = [plot];
                    plotName = plot.Name;
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
                        .Select(s => mapper.Map(s) with { PlotName = plot.Name });

                    result.AddRange(filtered);
                }

                var sorted = result
                    .OrderByDescending(s => s.ScheduledAt)
                    .ToList()
                    .AsReadOnly();

                var pdfBytes = pdfService.GenerateSprayingHistoryPdf(
                    sorted, plotName, request.From, request.To);

                logger.LogInformation("[PDF] Spraying history exported for farmer {OwnerId}, {Count} records.",
                    ownerId, sorted.Count);

                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PDF] Error exporting spraying history for farmer {OwnerId}.", ownerId);
                return Result<byte[]>.Failure("Failed to generate PDF.", ErrorType.Failure);
            }
        }
    }
}
