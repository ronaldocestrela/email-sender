using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Entities;
using EmailEngine.Domain.Enums;

namespace EmailEngine.Infrastructure.EmailSenders;

/// <summary>
/// Adapter concreto para envio físico de e-mails utilizando o protocolo SMTP via MailKit.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Envia o e-mail via conexão SMTP (usando configurações globais do sistema ou customizadas por Tenant).
    /// </summary>
    public async Task<Result> SendAsync(
        string to,
        string subject,
        string body,
        string senderAddress,
        string senderName,
        EmailProviderSettings? customSettings = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Padrões do Sistema
            var host = _configuration["SmtpSettings:Host"] ?? "localhost";
            var port = int.TryParse(_configuration["SmtpSettings:Port"], out var p) ? p : 1025;
            var username = _configuration["SmtpSettings:Username"] ?? string.Empty;
            var password = _configuration["SmtpSettings:Password"] ?? string.Empty;
            var enableSsl = bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) && ssl;

            // Sobrescreve se o Tenant possuir configurações customizadas SMTP
            if (customSettings != null && customSettings.Type == EmailProviderType.Smtp)
            {
                host = customSettings.SmtpHost;
                port = customSettings.SmtpPort;
                username = customSettings.SmtpUsername;
                password = customSettings.SmtpPassword;
                enableSsl = customSettings.SmtpEnableSsl;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderAddress));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Desabilita validação de certificado SSL no desenvolvimento local para facilidade de testes (ex: Mailpit)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(host, port, enableSsl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("Smtp.SendFailed", $"Falha ao enviar e-mail via SMTP: {ex.Message}"));
        }
    }
}
