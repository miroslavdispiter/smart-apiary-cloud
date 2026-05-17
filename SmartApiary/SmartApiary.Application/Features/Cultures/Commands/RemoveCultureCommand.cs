using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Cultures.Commands
{
    // COMMAND
    public record RemoveCultureCommand(string PlotId) : IRequest<Result>;

    // VALIDATOR
    public class RemoveCultureValidator : AbstractValidator<RemoveCultureCommand>
    {
        public RemoveCultureValidator()
        {
            RuleFor(x => x.PlotId).NotEmpty().WithMessage("Plot ID is required.");
        }
    }

    // HANDLER
    internal class RemoveCultureHandler(
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        ILogger<RemoveCultureHandler> logger) : IRequestHandler<RemoveCultureCommand, Result>
    {
        public async Task<Result> Handle(RemoveCultureCommand request, CancellationToken ct)
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

            var removeResult = plot.RemoveCulture();
            if (removeResult.IsFailure)
                return removeResult;

            try
            {
                await plotRepository.UpdateAsync(plot, ct);

                logger.LogInformation("[CULTURE] Culture removed from plot '{PlotId}' by farmer {OwnerId}.",
                    request.PlotId, ownerId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CULTURE] Error removing culture from plot '{PlotId}'.", request.PlotId);
                return Result.Failure("Failed to remove culture.", ErrorType.Failure);
            }
        }
    }
}
