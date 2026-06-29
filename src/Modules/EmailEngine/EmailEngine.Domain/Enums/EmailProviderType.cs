namespace EmailEngine.Domain.Enums;

/// <summary>
/// Tipos de provedores de envio de e-mail suportados.
/// </summary>
public enum EmailProviderType
{
    /// <summary>
    /// Envio via protocolo clássico SMTP.
    /// </summary>
    Smtp,

    /// <summary>
    /// Envio via API REST do SendGrid.
    /// </summary>
    SendGrid,

    /// <summary>
    /// Envio via API do AWS Simple Email Service (SES).
    /// </summary>
    AwsSes
}
