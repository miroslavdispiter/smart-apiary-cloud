using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartApiary.Application.Interfaces;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;
using SmartApiary.Infrastructure.Persistence.AzureQueue.Messages;

namespace SmartApiary.Functions.Ingestion;

public class TelemetryQueueFunction
{
    private const double DefaultWeightDropThresholdKg = 10.0;
    private const int LowBatteryThresholdPercent = 15;

    private readonly ITelemetrySampleRepository _telemetrySampleRepository;
    private readonly IHiveRepository _hiveRepository;
    private readonly IApiaryRepository _apiaryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<TelemetryQueueFunction> _logger;

    public TelemetryQueueFunction(
        ITelemetrySampleRepository telemetrySampleRepository,
        IHiveRepository hiveRepository,
        IApiaryRepository apiaryRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        IJsonSerializer serializer,
        ILogger<TelemetryQueueFunction> logger)
    {
        _telemetrySampleRepository = telemetrySampleRepository;
        _hiveRepository = hiveRepository;
        _apiaryRepository = apiaryRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _serializer = serializer;
        _logger = logger;
    }

    [Function("TelemetryQueueFunction")]
    public async Task Run([QueueTrigger("telemetry-queue")] string message, CancellationToken ct)
    {
        TelemetryQueueMessage? msg = _serializer.Deserialize<TelemetryQueueMessage>(message);

        if (msg is null)
        {
            _logger.LogError("Failed to deserialize telemetry message.");
            return;
        }

        var previousSample = await _telemetrySampleRepository.GetLatestByHiveIdAsync(msg.HiveId, ct);

        var sample = TelemetrySample.Create(
            msg.DeviceId,
            msg.HiveId,
            msg.WeightKg,
            msg.Humidity,
            msg.Temperature,
            msg.BatteryPercentage,
            msg.RecordedAt
        );

        if (sample.IsFailure)
        {
            _logger.LogError("Failed to create TelemetrySample: {Error}", sample.Error?.Message);
            return;
        }

        await _telemetrySampleRepository.AddAsync(sample.Value, ct);
        _logger.LogInformation("Telemetry saved for hive {HiveId}", msg.HiveId);

        await ProcessAlertsAsync(sample.Value, previousSample, ct);
    }

    private async Task ProcessAlertsAsync(TelemetrySample current, TelemetrySample? previous, CancellationToken ct)
    {
        if (previous is null)
            return;

        var weightDrop = previous.WeightKg - current.WeightKg;
        if (weightDrop >= DefaultWeightDropThresholdKg)
        {
            await SendBeekeeperAlertAsync(
                current.HiveId.Value,
                "Critical Smart Apiary alert - sudden hive weight drop",
                $"Hive weight dropped by {weightDrop:0.##} kg. Previous: {previous.WeightKg:0.##} kg, current: {current.WeightKg:0.##} kg. Possible theft or overturned hive.",
                ct);
        }

        if (previous.BatteryPercentage >= LowBatteryThresholdPercent && current.BatteryPercentage < LowBatteryThresholdPercent)
        {
            await SendBeekeeperAlertAsync(
                current.HiveId.Value,
                "Smart Apiary alert - low battery",
                $"Hive scale battery is low ({current.BatteryPercentage}%). Please replace or charge the battery.",
                ct);
        }
    }

    private async Task SendBeekeeperAlertAsync(string hiveId, string subject, string message, CancellationToken ct)
    {
        try
        {
            var hiveIdResult = EntityId.Create(hiveId);
            if (hiveIdResult.IsFailure)
                return;

            var hive = await _hiveRepository.GetByIdAsync(hiveIdResult.Value, ct);
            if (hive is null)
            {
                _logger.LogWarning("Alert skipped. Hive {HiveId} was not found.", hiveId);
                return;
            }

            var apiary = await _apiaryRepository.GetByIdAsync(hive.ApiaryId, ct);
            if (apiary is null)
            {
                _logger.LogWarning("Alert skipped. Apiary {ApiaryId} was not found.", hive.ApiaryId.Value);
                return;
            }

            var ownerIdResult = EntityId.Create(apiary.OwnerId);
            if (ownerIdResult.IsFailure)
                return;

            var owner = await _userRepository.GetByIdAsync(ownerIdResult.Value, ct);
            if (owner is null)
            {
                _logger.LogWarning("Alert skipped. Owner {OwnerId} was not found.", apiary.OwnerId);
                return;
            }

            await _emailService.SendAlertEmailAsync(
                owner.Email,
                subject,
                $"Hi {owner.FirstName},<br/><br/>{message}<br/><br/>Apiary: {apiary.Name}<br/>Hive: {hive.Label}<br/><br/>Smart Apiary",
            ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send telemetry alert for hive {HiveId}.", hiveId);
        }
    }
}
