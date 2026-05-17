using MediatR;
using SmartApiary.Application.Features.Spraying.DTOs;
using SmartApiary.Domain.Common;

namespace SmartApiary.Application.Features.Spraying.Queries.GetUpcomingSprayingsNearApiary
{
    public record GetUpcomingSprayingsNearApiaryQuery(string ApiaryId)
        : IRequest<Result<IReadOnlyCollection<UpcomingSprayingAlertDto>>>;
}