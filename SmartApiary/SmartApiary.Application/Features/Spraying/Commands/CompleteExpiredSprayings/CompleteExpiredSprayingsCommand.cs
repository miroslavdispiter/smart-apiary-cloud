using MediatR;
using SmartApiary.Domain.Common;

namespace SmartApiary.Application.Features.Spraying.Commands.CompleteExpiredSprayings
{
    public record CompleteExpiredSprayingsCommand : IRequest<Result<int>>;
}