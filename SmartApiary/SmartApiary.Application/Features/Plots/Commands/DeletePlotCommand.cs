using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Plots.Commands
{
    // COMMAND
    public record DeletePlotCommand(string PlotId) : IRequest<Result>;

    // VALIDATOR
    public class DeletePlotValidator : AbstractValidator<DeletePlotCommand>
    {
        public DeletePlotValidator()
        {
            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("Plot ID is required.");
        }
    }

    // HANDLER
    internal class DeletePlotHandler(
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        ILogger<DeletePlotHandler> logger) : IRequestHandler<DeletePlotCommand, Result>
    {
        public async Task<Result> Handle(DeletePlotCommand request, CancellationToken ct)
        {
            var ownerId = currentUserService.UserId;

            var idResult = EntityId.Create(request.PlotId);
            if (idResult.IsFailure)
                return Result.Failure(idResult.Error!.Message, ErrorType.Validation);

            var plot = await plotRepository.GetByIdAsync(idResult.Value, ct);
            if (plot is null)
                return Result.Failure("Plot not found.", ErrorType.NotFound);

            if (!plot.IsOwnedBy(ownerId))
                return Result.Failure("Access denied.", ErrorType.Forbidden);

            try
            {
                await plotRepository.DeleteAsync(idResult.Value, ct);

                logger.LogInformation("[PLOT] Plot '{Id}' deleted by farmer {OwnerId}.", request.PlotId, ownerId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PLOT] Error deleting plot '{Id}'.", request.PlotId);
                return Result.Failure("Failed to delete plot.", ErrorType.Failure);
            }
        }
    }
}
