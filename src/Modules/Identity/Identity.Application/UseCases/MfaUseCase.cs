using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Domain.Common;
using Identity.Domain.Aggregates;
using Identity.Application.Ports;

namespace Identity.Application.UseCases;

/// <summary>
/// DTO de resposta contendo os dados de configuração inicial do MFA.
/// </summary>
public record MfaSetupResponse(string Secret, string QrCodeUri);

/// <summary>
/// Interface do caso de uso de Multi-Factor Authentication.
/// </summary>
public interface IMfaUseCase
{
    /// <summary>
    /// Inicializa o setup do MFA gerando o segredo e o QR Code.
    /// </summary>
    Task<Result<MfaSetupResponse>> SetupMfaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma o código MFA inicial para habilitar o fator no perfil do usuário.
    /// </summary>
    Task<Result> ConfirmMfaAsync(Guid userId, string code, string secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desabilita o MFA do usuário.
    /// </summary>
    Task<Result> DisableMfaAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do caso de uso de Multi-Factor Authentication.
/// </summary>
public class MfaUseCase : IMfaUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IMfaService _mfaService;

    public MfaUseCase(IUserRepository userRepository, IMfaService mfaService)
    {
        _userRepository = userRepository;
        _mfaService = mfaService;
    }

    /// <summary>
    /// Inicializa a configuração do MFA gerando um novo segredo TOTP e a URI para o QR Code.
    /// </summary>
    public async Task<Result<MfaSetupResponse>> SetupMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result<MfaSetupResponse>.Failure(new Error("Mfa.UserNotFound", "Usuário não encontrado."));
        }

        var secret = _mfaService.GenerateMfaSecret();
        var qrCodeUri = _mfaService.GetQrCodeUri(user.Email, secret);

        return Result<MfaSetupResponse>.Success(new MfaSetupResponse(secret, qrCodeUri));
    }

    /// <summary>
    /// Valida o primeiro código gerado e ativa definitivamente o MFA para o usuário.
    /// </summary>
    public async Task<Result> ConfirmMfaAsync(Guid userId, string code, string secret, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(secret))
        {
            return Result.Failure(new Error("Mfa.InvalidRequest", "Código e segredo são obrigatórios."));
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(new Error("Mfa.UserNotFound", "Usuário não encontrado."));
        }

        var isCodeValid = _mfaService.VerifyCode(secret, code);
        if (!isCodeValid)
        {
            return Result.Failure(new Error("Mfa.InvalidCode", "Código de verificação incorreto ou expirado."));
        }

        var enableResult = user.EnableMfa(secret);
        if (enableResult.IsFailure)
        {
            return enableResult;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Desabilita o segundo fator de autenticação para o usuário.
    /// </summary>
    public async Task<Result> DisableMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(new Error("Mfa.UserNotFound", "Usuário não encontrado."));
        }

        var disableResult = user.DisableMfa();
        if (disableResult.IsFailure)
        {
            return disableResult;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        return Result.Success();
    }
}
