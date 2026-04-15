namespace Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string token, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string token, CancellationToken ct = default);
    Task SendInviteAsync(string toEmail, string orgName, string token, CancellationToken ct = default);
}
