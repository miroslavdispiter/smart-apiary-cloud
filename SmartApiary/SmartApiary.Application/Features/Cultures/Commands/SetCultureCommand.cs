using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Cultures.DTOs;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Common;
using SmartApiary.Domain.Enums;
using SmartApiary.Domain.Enums.Farmer;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Features.Cultures.Commands
{
    // COMMAND
    public record SetCultureCommand : IRequest<Result<CultureDto>>
    {
        public string PlotId { get; init; } = string.Empty;
        public CultureType Type { get; init; }
        public DateTime BloomingStart { get; init; }
        public DateTime BloomingEnd { get; init; }
        public string? AdditionalInfo { get; init; }
    }

    // VALIDATOR
    public class SetCultureValidator : AbstractValidator<SetCultureCommand>
    {
        public SetCultureValidator()
        {
            RuleFor(x => x.PlotId)
                .NotEmpty().WithMessage("Plot ID is required.");

            RuleFor(x => x.Type)
                .Must(t => t != CultureType.Unknown && Enum.IsDefined(typeof(CultureType), t))
                .WithMessage("Invalid culture type.");

            RuleFor(x => x.BloomingStart)
                .NotEmpty().WithMessage("Blooming start date is required.");

            RuleFor(x => x.BloomingEnd)
                .GreaterThan(x => x.BloomingStart).WithMessage("Blooming end must be after start date.");

            RuleFor(x => x.AdditionalInfo)
                .MaximumLength(500).WithMessage("Additional info must not exceed 500 characters.")
                .When(x => x.AdditionalInfo is not null);
        }
    }

    // HANDLER
    internal class SetCultureHandler(
        IPlotRepository plotRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper<Culture, CultureDto> mapper,
        ILogger<SetCultureHandler> logger) : IRequestHandler<SetCultureCommand, Result<CultureDto>>
    {
        public async Task<Result<CultureDto>> Handle(SetCultureCommand request, CancellationToken ct)
        {
            var now = dateTimeProvider.UtcNow;
            var ownerId = currentUserService.UserId;

            var idResult = EntityId.Create(request.PlotId);
            if (idResult.IsFailure)
                return Result<CultureDto>.Failure(idResult.Error!.Message, ErrorType.Validation);

            var plot = await plotRepository.GetByIdAsync(idResult.Value, ct);
            if (plot is null)
                return Result<CultureDto>.Failure("Plot not found.", ErrorType.NotFound);

            if (!plot.IsOwnedBy(ownerId))
                return Result<CultureDto>.Failure("Access denied.", ErrorType.Forbidden);

            var cultureResult = Culture.Create(
                request.PlotId,
                request.Type,
                request.BloomingStart,
                request.BloomingEnd,
                request.AdditionalInfo,
                now);

            if (cultureResult.IsFailure)
                return Result<CultureDto>.Failure(cultureResult.Error!.Message, ErrorType.Validation);

            var setResult = plot.SetCulture(cultureResult.Value);
            if (setResult.IsFailure)
                return Result<CultureDto>.Failure(setResult.Error!.Message, ErrorType.Validation);

            try
            {
                await plotRepository.UpdateAsync(plot, ct);

                logger.LogInformation("[CULTURE] Culture '{Type}' set on plot '{PlotId}' by farmer {OwnerId}.",
                    request.Type, request.PlotId, ownerId);

                return Result<CultureDto>.Success(mapper.Map(plot.Culture!));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CULTURE] Error setting culture on plot '{PlotId}'.", request.PlotId);
                return Result<CultureDto>.Failure("Failed to set culture.", ErrorType.Failure);
            }
        }
    }
}
