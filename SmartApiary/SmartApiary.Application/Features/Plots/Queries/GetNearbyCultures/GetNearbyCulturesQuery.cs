using MediatR;
using SmartApiary.Application.Features.Plots.DTOs;
using SmartApiary.Domain.Common;

namespace SmartApiary.Application.Features.Plots.Queries.GetNearbyCultures
{
    public record GetNearbyCulturesQuery(string ApiaryId)
        : IRequest<Result<IReadOnlyCollection<NearbyCultureDto>>>;
}