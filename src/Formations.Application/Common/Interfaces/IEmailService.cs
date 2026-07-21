namespace Formations.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendPasswordResetAsync(string to, string resetLink, CancellationToken ct = default);
    Task SendWelcomeAsync(string to, string fullName, CancellationToken ct = default);
    Task SendCertificateAsync(string to, string fullName, string formationTitle, byte[] pdfBytes, CancellationToken ct = default);
}
