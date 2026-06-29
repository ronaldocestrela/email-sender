using System;
using System.Text.RegularExpressions;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Enums;

namespace EmailEngine.Domain.Entities;

/// <summary>
/// Configurações customizadas do provedor de e-mail por Tenant.
/// </summary>
public class EmailProviderSettings : IMustHaveTenant
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Identificador único da configuração.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identificador do Tenant associado.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Tipo do provedor de e-mail.
    /// </summary>
    public EmailProviderType Type { get; private set; }

    /// <summary>
    /// Chave de API de acesso ao serviço (usada por SendGrid).
    /// </summary>
    public string ApiKey { get; private set; } = string.Empty;

    /// <summary>
    /// Endereço do host SMTP.
    /// </summary>
    public string SmtpHost { get; private set; } = string.Empty;

    /// <summary>
    /// Porta do servidor SMTP.
    /// </summary>
    public int SmtpPort { get; private set; }

    /// <summary>
    /// Nome de usuário do servidor SMTP.
    /// </summary>
    public string SmtpUsername { get; private set; } = string.Empty;

    /// <summary>
    /// Senha de acesso do servidor SMTP.
    /// </summary>
    public string SmtpPassword { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se deve utilizar conexão SSL/TLS.
    /// </summary>
    public bool SmtpEnableSsl { get; private set; }

    /// <summary>
    /// Endereço de e-mail padrão do remetente.
    /// </summary>
    public string SenderAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Nome amigável de exibição do remetente.
    /// </summary>
    public string SenderName { get; private set; } = string.Empty;

    private EmailProviderSettings() { }

    /// <summary>
    /// Cria uma configuração de provedor do tipo SMTP.
    /// </summary>
    public static Result<EmailProviderSettings> CreateSmtp(
        Guid tenantId,
        string host,
        int port,
        string username,
        string password,
        bool enableSsl,
        string senderAddress,
        string senderName)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidTenant", "O TenantId não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidSmtpHost", "O Host do SMTP não pode ser vazio."));
        }

        if (port <= 0 || port > 65535)
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidSmtpPort", "A porta do SMTP é inválida."));
        }

        if (string.IsNullOrWhiteSpace(senderAddress) || !EmailRegex.IsMatch(senderAddress.Trim()))
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidSenderAddress", "O endereço de e-mail do remetente é inválido."));
        }

        var settings = new EmailProviderSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = EmailProviderType.Smtp,
            SmtpHost = host.Trim(),
            SmtpPort = port,
            SmtpUsername = username?.Trim() ?? string.Empty,
            SmtpPassword = password ?? string.Empty,
            SmtpEnableSsl = enableSsl,
            SenderAddress = senderAddress.Trim().ToLowerInvariant(),
            SenderName = senderName?.Trim() ?? string.Empty
        };

        return Result<EmailProviderSettings>.Success(settings);
    }

    /// <summary>
    /// Cria uma configuração de provedor do tipo SendGrid.
    /// </summary>
    public static Result<EmailProviderSettings> CreateSendGrid(
        Guid tenantId,
        string apiKey,
        string senderAddress,
        string senderName)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidTenant", "O TenantId não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidApiKey", "A chave de API do SendGrid não pode ser vazia."));
        }

        if (string.IsNullOrWhiteSpace(senderAddress) || !EmailRegex.IsMatch(senderAddress.Trim()))
        {
            return Result<EmailProviderSettings>.Failure(new Error("EmailProviderSettings.InvalidSenderAddress", "O endereço de e-mail do remetente é inválido."));
        }

        var settings = new EmailProviderSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = EmailProviderType.SendGrid,
            ApiKey = apiKey.Trim(),
            SenderAddress = senderAddress.Trim().ToLowerInvariant(),
            SenderName = senderName?.Trim() ?? string.Empty
        };

        return Result<EmailProviderSettings>.Success(settings);
    }
}
