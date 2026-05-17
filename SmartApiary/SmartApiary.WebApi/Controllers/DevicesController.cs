using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using SmartApiary.Application.Features.Devices.Commands.Registration;
using SmartApiary.Application.Features.Telemetry.Commands;
using SmartApiary.Application.Features.Users.Commands.Login;
using SmartApiary.Domain.Common;
using SmartApiary.WebApi.DTOs;
using SmartApiary.WebApi.Extensions;

namespace SmartApiary.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly IMediator _mediator;
        
        public DevicesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Beekeeper")]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceCommand command, CancellationToken ct)
        {
            Result result = await _mediator.Send(command, ct);
            return result.ToActionResult();
        }

        [HttpPost("telemetry")]
        public async Task<IActionResult> Telemetry([FromBody] TelemetryRequest request, CancellationToken ct)
        {
            var token = Request.Headers["X-Device-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(token)) return Unauthorized("Missing X-Device-Token header.");

            var result = await _mediator.Send(new IngestTelemetryCommand
            {
                AccessToken = token,
                WeightKg = request.WeightKg,
                Humidity = request.Humidity,
                Temperature = request.Temperature,
                BatteryPercentage = request.BatteryPercentage,
                RecordedAt = request.RecordedAt
            }, ct);

            return result.ToActionResult();
        }
    }
}
