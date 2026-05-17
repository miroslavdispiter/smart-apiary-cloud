using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartApiary.Application.Features.Apiaries.Commands.CreateApiary;
using SmartApiary.Application.Features.Apiaries.Commands.DeleteApiary;
using SmartApiary.Application.Features.Apiaries.Commands.UpdateApiary;
using SmartApiary.Application.Features.Apiaries.Commands.UploadApiaryImage;
using SmartApiary.Application.Features.Apiaries.Queries.GetApiaryById;
using SmartApiary.Application.Features.Apiaries.Queries.GetMyApiaries;
using SmartApiary.Application.Features.Plots.Queries.GetNearbyCultures;
using SmartApiary.Application.Features.Spraying.Queries.GetUpcomingSprayingsNearApiary;
using SmartApiary.WebApi.DTOs;
using SmartApiary.WebApi.Extensions;

namespace SmartApiary.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Beekeeper")]
    public class ApiariesController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetMyApiaries(CancellationToken ct)
        {
            var result = await mediator.Send(new GetMyApiariesQuery(), ct);
            return result.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetApiaryByIdQuery(id), ct);
            return result.ToActionResult();
        }

        [HttpGet("{id}/upcoming-sprayings")]
        public async Task<IActionResult> GetUpcomingSprayings(string id, CancellationToken ct)
        {
            var result = await mediator.Send(
                new GetUpcomingSprayingsNearApiaryQuery(id), ct);

            return result.ToActionResult();
        }

        [HttpGet("{id}/nearby-cultures")]
        public async Task<IActionResult> GetNearbyCultures(string id, CancellationToken ct)
        {
            var result = await mediator.Send(
                new GetNearbyCulturesQuery(id), ct);

            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateApiaryCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateApiaryRequest request, CancellationToken ct)
        {
            var command = new UpdateApiaryCommand
            {
                ApiaryId = id,
                Name = request.Name,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Description = request.Description
            };

            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            var result = await mediator.Send(new DeleteApiaryCommand(id), ct);
            return result.ToActionResult();
        }

        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadImage(string id, IFormFile image, CancellationToken ct)
        {
            if (image is null || image.Length == 0)
                return BadRequest(new { message = "No image file provided." });

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms, ct);

            var command = new UploadApiaryImageCommand
            {
                ApiaryId = id,
                ImageData = ms.ToArray(),
                ContentType = image.ContentType
            };

            var result = await mediator.Send(command, ct);
            return result.ToActionResult();
        }
    }
}