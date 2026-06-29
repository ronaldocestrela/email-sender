using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Entities;
using EmailEngine.Domain.Enums;

namespace EmailEngine.Infrastructure.EmailSenders;

/// <summary>
/// Orquestrador de envio que delega a execução para o adapter correto (SMTP ou SendGrid)
/// com base nas configurações customizadas do Tenant ou padrões do sistema.
/// </summary>
public class CompositeEmailSender : IEmailSender
{
    private readonly SmtpEmailSender _smtpEmailSender;
    private readonly SendGridEmailSender _sendGridEmailSender;
    private readonly IConfiguration _configuration;

    public CompositeEmailSender(
        SmtpEmailSender smtpEmailSender,
        SendGridEmailSender sendGridEmailSender,
        IConfiguration configuration)
    {
        _smtpEmailSender = smtpEmailSender;
        _sendGridEmailSender = sendGridEmailSender;
        _configuration = configuration;
    }

    /// <summary>
    /// Encaminha o envio para o provedor resolvido dinamicamente.
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
        // 1. Caso existam configurações customizadas do Tenant, usa o tipo especificado
        if (customSettings != null)
        {
            return customSettings.Type switch
            {
                EmailProviderType.SendGrid => await _sendGridEmailSender.SendAsync(to, subject, body, senderAddress, senderName, customSettings, cancellationToken),
                EmailProviderType.Smtp => await _smtpEmailSender.SendAsync(to, subject, body, senderAddress, senderName, customSettings, cancellationToken),
                _ => Result.Failure(new Error("EmailSender.UnsupportedType", $"O tipo de provedor {customSettings.Type} não é suportado."))
            };
        }

        // 2. Se não houver configurações do Tenant, cai para as configurações padrão do sistema
        var defaultProvider = _configuration["EmailSettings:Provider"] ?? "Smtp";

        if (defaultProvider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            return await _sendGridEmailSender.SendAsync(to, subject, body, senderAddress, senderName, null, cancellationToken);
        }

        return await _smtpEmailSender.SendAsync(to, subject, body, senderAddress, senderName, null, cancellationToken);
    }
}
