using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartApiary.Application.Features.Spraying.Commands;
using SmartApiary.Application.Features.Spraying.Queries;
using SmartApiary.WebApi.DTOs;
using SmartApiary.WebApi.Extensions;

namespace SmartApiary.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Farmer")]
    public class SprayingController(IMediator mediator) : ControllerBase
    {
        // GET api/spraying/history?plotId=&from=&to=&completedOnly=
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string? plotId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] bool completedOnly,
            CancellationToken ct)
        {
            var query = new GetSprayingHistoryQuery
            {
                PlotId        = plotId,
                From          = from,
                To            = to,
                CompletedOnly = completedOnly
            };
            var result = await mediator.Send(query, ct);
            return result.ToActionResult();
        }

        // POST api/spraying/{plotId}
        [HttpPost("{plotId}")]
        public async Task<IActionResult> Schedule(string plotId, [FromBody] ScheduleSprayingRequest request, CancellationToken ct)
        {
            var command = new ScheduleSprayingCommand
            {
                PlotId        = plotId,
                ScheduledAt   = request.ScheduledAt,
                DurationHours = request.DurationHours,
                Preparation   = request.Preparation
            };
            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        // PUT api/spraying/{plotId}/{scheduleId}/reschedule
        [HttpPut("{plotId}/{scheduleId}/reschedule")]
        public async Task<IActionResult> Reschedule(
            string plotId,
            string scheduleId,
            [FromBody] RescheduleSprayingRequest request,
            CancellationToken ct)
        {
            var command = new RescheduleSprayingCommand
            {
                PlotId          = plotId,
                ScheduleId      = scheduleId,
                NewScheduledAt  = request.NewScheduledAt,
                NewDurationHours = request.NewDurationHours
            };
            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        // DELETE api/spraying/{plotId}/{scheduleId}
        [HttpDelete("{plotId}/{scheduleId}")]
        public async Task<IActionResult> Cancel(string plotId, string scheduleId, CancellationToken ct)
        {
            var result = await mediator.Send(new CancelSprayingCommand(plotId, scheduleId), ct);
            return result.ToActionResult();
        }
    }
}
