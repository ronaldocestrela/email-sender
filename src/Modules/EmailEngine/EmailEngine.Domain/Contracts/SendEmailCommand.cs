using System;
using System.Collections.Generic;

namespace EmailEngine.Domain.Contracts;

/// <summary>
/// Comando imutável enviado para processamento e envio assíncrono de e-mails.
/// </summary>
public record SendEmailCommand(
    Guid TenantId,
    string To,
    string Subject,
    string Body,
    string SenderDomain,
    Dictionary<string, string> TemplateVariables
);
