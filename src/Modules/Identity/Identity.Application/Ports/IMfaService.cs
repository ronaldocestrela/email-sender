namespace Identity.Application.Ports;

/// <summary>
/// Port de saída para controle de Multi-Factor Authentication (TOTP).
/// </summary>
public interface IMfaService
{
    /// <summary>
    /// Gera um segredo aleatório em formato Base32 para TOTP.
    /// </summary>
    string GenerateMfaSecret();

    /// <summary>
    /// Gera o URI para exibição do QR Code de sincronização do MFA.
    /// </summary>
    string GetQrCodeUri(string email, string secret, string issuer = "EmailSender");

    /// <summary>
    /// Valida se o código fornecido pelo usuário é válido para o segredo informado.
    /// </summary>
    bool VerifyCode(string secret, string code);
}
