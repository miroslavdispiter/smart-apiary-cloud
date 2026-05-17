using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Plots.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;


namespace SmartApiary.Application.Features.Plots.Commands
{
    // COMMAND
    public record UpdatePlotCommand : IRequest<Result<PlotDto>>
    {
        public string PlotId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    // VALIDATOR
    public class UpdatePlotValidator : AbstractValidator<UpdatePlotCommand>
    {
        public UpdatePlotValidator()
        {
            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("Plot ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Plot name is required.")
                .MaximumLength(100).WithMessage("Plot name must not exceed 100 characters.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
        }
    }

    // HANDLER
    internal class UpdatePlotHandler(
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper<Plot, PlotDto> mapper,
        ILogger<UpdatePlotHandler> logger) : IRequestHandler<UpdatePlotCommand, Result<PlotDto>>
    {
        public async Task<Result<PlotDto>> Handle(UpdatePlotCommand request, CancellationToken ct)
        {
            var ownerId = currentUserService.UserId;

            var idResult = EntityId.Create(request.PlotId);
            if (idResult.IsFailure)
                return Result<PlotDto>.Failure(idResult.Error!.Message, ErrorType.Validation);

            var plot = await plotRepository.GetByIdAsync(idResult.Value, ct);
            if (plot is null)
                return Result<PlotDto>.Failure("Plot not found.", ErrorType.NotFound);

            if (!plot.IsOwnedBy(ownerId))
                return Result<PlotDto>.Failure("Access denied.", ErrorType.Forbidden);

            var updateResult = plot.UpdateLocation(request.Name, request.Latitude, request.Longitude);
            if (updateResult.IsFailure)
                return Result<PlotDto>.Failure(updateResult.Error!.Message, ErrorType.Validation);

            try
            {
                await plotRepository.UpdateAsync(plot, ct);

                logger.LogInformation("[PLOT] Plot '{Id}' updated by farmer {OwnerId}.", plot.Id.Value, ownerId);

                return Result<PlotDto>.Success(mapper.Map(plot));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PLOT] Error updating plot '{Id}'.", request.PlotId);
                return Result<PlotDto>.Failure("Failed to update plot.", ErrorType.Failure);
            }
        }
    }
}
