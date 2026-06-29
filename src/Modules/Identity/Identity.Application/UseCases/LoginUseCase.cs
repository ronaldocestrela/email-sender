using System.Threading;
using System.Threading.Tasks;
using Identity.Domain.Common;
using Identity.Domain.Aggregates;
using Identity.Application.Ports;

namespace Identity.Application.UseCases;

/// <summary>
/// DTO de resposta para o caso de uso de Login.
/// </summary>
public record LoginResponse(string? Token, bool RequiresMfa);

/// <summary>
/// Interface do caso de uso de Login.
/// </summary>
public interface ILoginUseCase
{
    /// <summary>
    /// Executa a autenticação do usuário.
    /// </summary>
    Task<Result<LoginResponse>> ExecuteAsync(string email, string password, string? mfaCode = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do caso de uso de Login.
/// </summary>
public class LoginUseCase : ILoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMfaService _mfaService;
    private readonly ITokenService _tokenService;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMfaService mfaService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mfaService = mfaService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Realiza a autenticação do usuário e retorna o token JWT correspondente.
    /// </summary>
    public async Task<Result<LoginResponse>> ExecuteAsync(
        string email,
        string password,
        string? mfaCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Result<LoginResponse>.Failure(new Error("Login.InvalidRequest", "E-mail e senha são obrigatórios."));
        }

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user == null)
        {
            return Result<LoginResponse>.Failure(new Error("Login.InvalidCredentials", "E-mail ou senha incorretos."));
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, password);
        if (!isPasswordValid)
        {
            return Result<LoginResponse>.Failure(new Error("Login.InvalidCredentials", "E-mail ou senha incorretos."));
        }

        if (user.IsMfaEnabled)
        {
            if (string.IsNullOrWhiteSpace(mfaCode))
            {
                // Usuário precisa fornecer código MFA
                return Result<LoginResponse>.Success(new LoginResponse(null, true));
            }

            var isMfaValid = _mfaService.VerifyCode(user.MfaSecret, mfaCode);
            if (!isMfaValid)
            {
                return Result<LoginResponse>.Failure(new Error("Login.InvalidMfaCode", "Código MFA incorreto ou expirado."));
            }
        }

        var token = _tokenService.GenerateToken(user);
        return Result<LoginResponse>.Success(new LoginResponse(token, false));
    }
}
