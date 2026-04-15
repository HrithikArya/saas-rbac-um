using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient http, IConfiguration config, ILogger<EmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string token, CancellationToken ct = default)
    {
        var appUrl = _config["App:Url"] ?? "http://localhost:3000";
        var verifyUrl = $"{appUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        await SendAsync(toEmail, "Verify your email", $"""
            <p>Please verify your email address by clicking the link below:</p>
            <p><a href="{verifyUrl}">Verify Email</a></p>
            <p>This link expires in 24 hours.</p>
            """, ct);
    }

    public async Task SendPasswordResetAsync(string toEmail, string token, CancellationToken ct = default)
    {
        var appUrl = _config["App:Url"] ?? "http://localhost:3000";
        var resetUrl = $"{appUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        await SendAsync(toEmail, "Reset your password", $"""
            <p>You requested a password reset. Click the link below to proceed:</p>
            <p><a href="{resetUrl}">Reset Password</a></p>
            <p>This link expires in 1 hour. If you did not request this, ignore this email.</p>
            """, ct);
    }

    public async Task SendInviteAsync(string toEmail, string orgName, string token, CancellationToken ct = default)
    {
        var appUrl = _config["App:Url"] ?? "http://localhost:3000";
        var acceptUrl = $"{appUrl}/invites/accept?token={Uri.EscapeDataString(token)}";

        await SendAsync(toEmail, $"You're invited to join {orgName}", $"""
            <p>You have been invited to join <strong>{orgName}</strong>.</p>
            <p><a href="{acceptUrl}">Accept Invitation</a></p>
            <p>This invitation expires in 48 hours.</p>
            """, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var apiKey = _config["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Resend API key not configured — skipping email to {Email} (subject: {Subject})", toEmail, subject);
            return;
        }

        var fromEmail = _config["Resend:FromEmail"] ?? "noreply@example.com";

        var payload = new
        {
            from = fromEmail,
            to = new[] { toEmail },
            subject,
            html = htmlBody
        };

        try
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _http.PostAsJsonAsync("https://api.resend.com/emails", payload, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
