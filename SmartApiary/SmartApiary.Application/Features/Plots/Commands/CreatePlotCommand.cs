using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Plots.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Models;

namespace SmartApiary.Application.Features.Plots.Commands
{
    // COMMAND
    public record CreatePlotCommand : IRequest<Result<PlotDto>>
    {
        public string Name { get; init; } = string.Empty;
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    // VALIDATOR
    public class CreatePlotValidator : AbstractValidator<CreatePlotCommand>
    {
        public CreatePlotValidator()
        {
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
    internal class CreatePlotHandler(
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper<Plot, PlotDto> mapper,
        ILogger<CreatePlotHandler> logger) : IRequestHandler<CreatePlotCommand, Result<PlotDto>>
    {
        public async Task<Result<PlotDto>> Handle(CreatePlotCommand request, CancellationToken ct)
        {
            var now = dateTimeProvider.UtcNow;
            var ownerId = currentUserService.UserId;

            var plotResult = Plot.Create(ownerId, request.Name, request.Latitude, request.Longitude, now);

            if (plotResult.IsFailure)
                return Result<PlotDto>.Failure(plotResult.Error!.Message, ErrorType.Validation);

            var plot = plotResult.Value;

            try
            {
                await plotRepository.AddAsync(plot, ct);

                logger.LogInformation("[PLOT] Plot '{Name}' (ID: {Id}) created by farmer {OwnerId}.",
                    plot.Name, plot.Id.Value, ownerId);

                return Result<PlotDto>.Success(mapper.Map(plot));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PLOT] Error creating plot '{Name}' for farmer {OwnerId}.", request.Name, ownerId);
                return Result<PlotDto>.Failure("Failed to create plot.", ErrorType.Failure);
            }
        }
    }
}