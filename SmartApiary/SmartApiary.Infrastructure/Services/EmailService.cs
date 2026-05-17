using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartApiary.Application.Common.Options;
using SmartApiary.Application.Interfaces;
using System.Net.Mail;

namespace SmartApiary.Infrastructure.Services;

internal class EmailService(
    IOptions<EmailOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendActivationEmailAsync(
        string email,
        string firstName,
        string activationLink,
        CancellationToken ct = default)
    {
        await SendEmailAsync(
            email,
            "Activate your Smart Apiary account",
            $"Hi {firstName},<br/><br/>" +
            $"Click the link below to activate your account:<br/>" +
            $"<a href='{activationLink}'>Activate Account</a><br/><br/>" +
            $"Link expires in 24 hours.",
            ct);
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken ct = default)
    {
        await SendEmailAsync(
            email,
            "Reset your Smart Apiary password",
            $"Hi {firstName},<br/><br/>" +
            $"Click the link below to reset your password:<br/>" +
            $"<a href='{resetLink}'>Reset Password</a><br/><br/>" +
            $"Link expires in 1 hour.",
            ct);
    }


    public async Task SendWelcomeEmailAsync(
    string email,
    string firstName,
    CancellationToken ct = default)
    {
        await SendEmailAsync(
            email,
            "Welcome to Smart Apiary!",
            $"Hi {firstName},<br/><br/>" +
            $"Your account has been successfully activated.<br/>" +
            $"You can now log in and start using Smart Apiary.<br/><br/>" +
            $"Happy beekeeping!<br/>" +
            $"Smart Apiary Team",
            ct);
    }


    public async Task SendAlertEmailAsync(
        string email,
        string subject,
        string htmlMessage,
        CancellationToken ct = default)
    {
        await SendEmailAsync(email, subject, htmlMessage, ct);
    }

    private async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken ct)
    {
        var client = new SendGridClient(_options.ApiKey);
        var from = new EmailAddress(_options.SenderEmail, _options.SenderName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);

        var response = await client.SendEmailAsync(msg, ct);

        if (!response.IsSuccessStatusCode)
            logger.LogError("[EMAIL] Failed to send email to {Email}. Status: {Status}",
                toEmail, response.StatusCode);
        else
            logger.LogInformation("[EMAIL] Email sent to {Email}.", toEmail);
    }
    public async Task SendSprayingAnnouncementAsync(
        string email,
        string plotName,
        DateTime scheduledAt,
        int durationHours,
        string? preparation,
        CancellationToken ct = default)
    {
        var prepInfo = preparation is not null
            ? $"<br/>Preparat: <strong>{preparation}</strong>"
            : string.Empty;

        await SendEmailAsync(
            email,
            "⚠️ Najava prskanja u blizini vašeg pčelinjaka",
            $"Poljoprivrednik je najavio tretiranje pesticidima u blizini vašeg pčelinjaka.<br/><br/>" +
            $"Parcela: <strong>{plotName}</strong><br/>" +
            $"Datum i vrijeme: <strong>{scheduledAt:dd.MM.yyyy HH:mm}</strong><br/>" +
            $"Trajanje: <strong>{durationHours}h</strong>" +
            $"{prepInfo}<br/><br/>" +
            $"Molimo vas da preduzmete potrebne mjere zaštite vaših pčela.<br/>" +
            $"Smart Apiary Tim",
            ct);
    }

    public async Task SendSprayingCancellationAsync(
        string email,
        string plotName,
        DateTime originalScheduledAt,
        CancellationToken ct = default)
    {
        await SendEmailAsync(
            email,
            "✅ Prskanje otkazano",
            $"Prethodno najavljeno tretiranje pesticidima je <strong>otkazano</strong>.<br/><br/>" +
            $"Parcela: <strong>{plotName}</strong><br/>" +
            $"Bilo zakazano za: <strong>{originalScheduledAt:dd.MM.yyyy HH:mm}</strong><br/><br/>" +
            $"Nije potrebna nikakva akcija s vaše strane.<br/>" +
            $"Smart Apiary Tim",
            ct);
    }

    public async Task SendSprayingRescheduledAsync(
        string email,
        string plotName,
        DateTime newScheduledAt,
        int newDurationHours,
        CancellationToken ct = default)
    {
        await SendEmailAsync(
            email,
            "🔄 Prskanje pomjereno",
            $"Tretiranje pesticidima u blizini vašeg pčelinjaka je <strong>pomjereno</strong>.<br/><br/>" +
            $"Parcela: <strong>{plotName}</strong><br/>" +
            $"Novi datum i vrijeme: <strong>{newScheduledAt:dd.MM.yyyy HH:mm}</strong><br/>" +
            $"Trajanje: <strong>{newDurationHours}h</strong><br/><br/>" +
            $"Molimo ažurirajte svoje mjere zaštite.<br/>" +
            $"Smart Apiary Tim",
            ct);
    }

}