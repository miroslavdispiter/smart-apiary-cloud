using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartApiary.Application.Features.Cultures.Commands;
using SmartApiary.Application.Features.Plots.Commands;
using SmartApiary.Application.Features.Plots.Queries;
using SmartApiary.WebApi.DTOs;
using SmartApiary.WebApi.Extensions;

namespace SmartApiary.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Farmer")]
    public class PlotsController(IMediator mediator) : ControllerBase
    {
        // GET api/plots
        [HttpGet]
        public async Task<IActionResult> GetMyPlots(CancellationToken ct)
        {
            var result = await mediator.Send(new GetMyPlotsQuery(), ct);
            return result.ToActionResult();
        }

        // POST api/plots
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePlotCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        // PUT api/plots/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePlotRequest request, CancellationToken ct)
        {
            var command = new UpdatePlotCommand
            {
                PlotId    = id,
                Name      = request.Name,
                Latitude  = request.Latitude,
                Longitude = request.Longitude
            };
            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        // DELETE api/plots/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            var result = await mediator.Send(new DeletePlotCommand(id), ct);
            return result.ToActionResult();
        }

        // POST api/plots/{id}/culture
        [HttpPost("{id}/culture")]
        public async Task<IActionResult> SetCulture(string id, [FromBody] SetCultureRequest request, CancellationToken ct)
        {
            var command = new SetCultureCommand
            {
                PlotId        = id,
                Type          = request.Type,
                BloomingStart = request.BloomingStart,
                BloomingEnd   = request.BloomingEnd,
                AdditionalInfo = request.AdditionalInfo
            };
            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        // DELETE api/plots/{id}/culture
        [HttpDelete("{id}/culture")]
        public async Task<IActionResult> RemoveCulture(string id, CancellationToken ct)
        {
            var result = await mediator.Send(new RemoveCultureCommand(id), ct);
            return result.ToActionResult();
        }
    }
}
