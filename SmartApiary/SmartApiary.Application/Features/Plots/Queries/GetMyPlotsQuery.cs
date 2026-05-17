using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Plots.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Models;

namespace SmartApiary.Application.Features.Plots.Queries
{
    // QUERY
    public record GetMyPlotsQuery : IRequest<Result<IReadOnlyCollection<PlotDto>>>;

    // HANDLER
    internal class GetMyPlotsHandler(
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        IMapper<Plot, PlotDto> mapper,
        ILogger<GetMyPlotsHandler> logger) : IRequestHandler<GetMyPlotsQuery, Result<IReadOnlyCollection<PlotDto>>>
    {
        public async Task<Result<IReadOnlyCollection<PlotDto>>> Handle(GetMyPlotsQuery request, CancellationToken ct)
        {
            try
            {
                var plots = await plotRepository.GetAllByOwnerAsync(currentUserService.UserId, ct);
                var dtos = plots.Select(mapper.Map).ToList().AsReadOnly();
                return Result<IReadOnlyCollection<PlotDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PLOT] Error fetching plots for farmer {OwnerId}.", currentUserService.UserId);
                return Result<IReadOnlyCollection<PlotDto>>.Failure("Failed to fetch plots.", ErrorType.Failure);
            }
        }
    }
}
