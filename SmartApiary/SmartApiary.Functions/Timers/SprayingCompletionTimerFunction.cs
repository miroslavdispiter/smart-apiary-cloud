using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Features.Spraying.Commands.CompleteExpiredSprayings;

namespace SmartApiary.Functions.Timers
{
    public class SprayingCompletionTimerFunction
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SprayingCompletionTimerFunction> _logger;

        public SprayingCompletionTimerFunction(
            IMediator mediator,
            ILogger<SprayingCompletionTimerFunction> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [Function("SprayingCompletionTimer")]
        public async Task Run(
            [TimerTrigger("*/30 * * * * *")] TimerInfo timerInfo,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "[TIMER] SprayingCompletionTimer triggered at {Time}.",
                DateTime.UtcNow);

            var result = await _mediator.Send(
                new CompleteExpiredSprayingsCommand(), ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[TIMER] Completed {Count} expired sprayings.",
                    result.Value);
            }
            else
            {
                _logger.LogError(
                    "[TIMER] Failed to complete expired sprayings: {Error}",
                    result.Error?.Message);
            }
        }
    }
}