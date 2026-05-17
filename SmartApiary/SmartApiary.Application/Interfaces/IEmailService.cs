namespace SmartApiary.Application.Interfaces;

public interface IEmailService
{
    Task SendActivationEmailAsync(
        string email,
        string firstName,
        string activationLink,
        CancellationToken ct = default);

    Task SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken ct = default);

    Task SendWelcomeEmailAsync(
    string email,
    string firstName,
    CancellationToken ct = default);

    Task SendAlertEmailAsync(
        string email,
        string subject,
        string htmlMessage,
        CancellationToken ct = default);

    Task SendSprayingAnnouncementAsync(
            string email,
            string plotName,
            DateTime scheduledAt,
            int durationHours,
            string? preparation,
            CancellationToken ct = default);

    Task SendSprayingCancellationAsync(
        string email,
        string plotName,
        DateTime originalScheduledAt,
        CancellationToken ct = default);

    Task SendSprayingRescheduledAsync(
        string email,
        string plotName,
        DateTime newScheduledAt,
        int newDurationHours,
        CancellationToken ct = default);
}