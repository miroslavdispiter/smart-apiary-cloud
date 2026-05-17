using SmartApiary.Application.Interfaces;

namespace SmartApiary.Functions.Services;

public sealed class NoOpEmailService : IEmailService
{
    public Task SendActivationEmailAsync(
        string email,
        string firstName,
        string activationLink,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendWelcomeEmailAsync(
        string email,
        string firstName,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendAlertEmailAsync(
        string email,
        string subject,
        string htmlMessage,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendSprayingAnnouncementAsync(
        string email,
        string plotName,
        DateTime scheduledAt,
        int durationHours,
        string? preparation,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendSprayingCancellationAsync(
        string email,
        string plotName,
        DateTime originalScheduledAt,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendSprayingRescheduledAsync(
        string email,
        string plotName,
        DateTime newScheduledAt,
        int newDurationHours,
        CancellationToken ct = default)
        => Task.CompletedTask;
}