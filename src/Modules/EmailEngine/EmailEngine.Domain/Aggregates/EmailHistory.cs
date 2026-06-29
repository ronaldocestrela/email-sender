using System;
using System.Text.RegularExpressions;
using EmailEngine.Domain.Common;

namespace EmailEngine.Domain.Aggregates;

/// <summary>
/// Registro histórico de auditoria para disparos de e-mail por Tenant.
/// </summary>
public class EmailHistory : IMustHaveTenant
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Identificador único do log de e-mail.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identificador do Tenant a que pertence o envio.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Destinatário do e-mail.
    /// </summary>
    public string To { get; private set; } = string.Empty;

    /// <summary>
    /// Assunto do e-mail.
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Corpo/Conteúdo do e-mail (geralmente HTML).
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Domínio de envio autenticado.
    /// </summary>
    public string SenderDomain { get; private set; } = string.Empty;

    /// <summary>
    /// Data/Hora em que o envio foi efetuado.
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Indica se o envio físico foi bem-sucedido.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Mensagem de erro caso o envio tenha falhado.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private EmailHistory() { }

    /// <summary>
    /// Cria e valida uma nova entrada de histórico de disparo.
    /// </summary>
    public static Result<EmailHistory> Create(
        Guid tenantId,
        string to,
        string subject,
        string body,
        string senderDomain,
        bool isSuccess,
        string? errorMessage = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<EmailHistory>.Failure(new Error("EmailHistory.InvalidTenant", "O TenantId não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(to) || !EmailRegex.IsMatch(to.Trim()))
        {
            return Result<EmailHistory>.Failure(new Error("EmailHistory.InvalidRecipient", "O destinatário do e-mail é inválido."));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result<EmailHistory>.Failure(new Error("EmailHistory.InvalidSubject", "O assunto do e-mail não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Result<EmailHistory>.Failure(new Error("EmailHistory.InvalidBody", "O corpo do e-mail não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(senderDomain))
        {
            return Result<EmailHistory>.Failure(new Error("EmailHistory.InvalidSenderDomain", "O domínio do remetente não pode ser vazio."));
        }

        var history = new EmailHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            To = to.Trim().ToLowerInvariant(),
            Subject = subject.Trim(),
            Body = body,
            SenderDomain = senderDomain.Trim().ToLowerInvariant(),
            SentAt = DateTime.UtcNow,
            IsSuccess = isSuccess,
            ErrorMessage = isSuccess ? null : errorMessage?.Trim()
        };

        return Result<EmailHistory>.Success(history);
    }
}
