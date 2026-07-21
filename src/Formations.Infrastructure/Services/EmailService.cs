using Formations.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Formations.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Email:FromName"] ?? "Formations", _config["Email:From"]!));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:Host"],
                int.Parse(_config["Email:Port"] ?? "587"),
                SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"], ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email envoyé à {To} : {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur d'envoi email à {To}", to);
        }
    }

    public Task SendPasswordResetAsync(string to, string resetLink, CancellationToken ct = default)
    {
        var html = $"""
            <h2>Réinitialisation de mot de passe</h2>
            <p>Cliquez sur le lien ci-dessous pour réinitialiser votre mot de passe :</p>
            <a href="{resetLink}" style="background:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;">
                Réinitialiser mon mot de passe
            </a>
            <p>Ce lien expire dans 1 heure. Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.</p>
            """;
        return SendEmailAsync(to, "Réinitialisation de votre mot de passe", html, ct);
    }

    public Task SendWelcomeAsync(string to, string fullName, CancellationToken ct = default)
    {
        var html = $"""
            <h2>Bienvenue sur la Plateforme Formations, {fullName} !</h2>
            <p>Votre compte a été créé avec succès. Vous pouvez dès maintenant explorer nos formations.</p>
            <a href="{_config["App:BaseUrl"]}/formations" style="background:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;">
                Découvrir les formations
            </a>
            """;
        return SendEmailAsync(to, "Bienvenue sur la Plateforme Formations", html, ct);
    }

    public async Task SendCertificateAsync(string to, string fullName, string formationTitle, byte[] pdfBytes, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config["Email:FromName"], _config["Email:From"]!));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = $"Votre certificat : {formationTitle}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"<h2>Félicitations {fullName} !</h2><p>Vous trouverez votre certificat en pièce jointe.</p>"
        };
        builder.Attachments.Add($"certificat_{formationTitle.Replace(" ", "_")}.pdf", pdfBytes, new ContentType("application", "pdf"));
        message.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"] ?? "587"), SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"], ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi certificat à {To}", to);
        }
    }
}
