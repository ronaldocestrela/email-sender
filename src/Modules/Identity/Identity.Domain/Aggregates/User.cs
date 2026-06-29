using System;
using System.Linq;
using System.Text.RegularExpressions;
using Identity.Domain.Common;

namespace Identity.Domain.Aggregates;

/// <summary>
/// Raiz do Agregado User (Usuário) contendo dados cadastrais e configurações de segurança/MFA.
/// </summary>
public class User : IMustHaveTenant
{
    private static readonly string[] ValidRoles = { "Admin", "User", "Operator" };

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Identificador único do Usuário.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Endereço de e-mail do usuário.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Hash seguro da senha do usuário.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador do Tenant a que o usuário pertence para isolamento lógico.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Papel de segurança do usuário (ex: Admin, User, Operator).
    /// </summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se o MFA (Multi-Factor Authentication) está habilitado.
    /// </summary>
    public bool IsMfaEnabled { get; private set; }

    /// <summary>
    /// Segredo TOTP utilizado para a geração do token MFA.
    /// </summary>
    public string MfaSecret { get; private set; } = string.Empty;

    /// <summary>
    /// Data de criação do registro de usuário.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private User() { }

    /// <summary>
    /// Cria um novo usuário após validar as invariantes de negócio.
    /// </summary>
    /// <param name="email">Endereço de e-mail do usuário.</param>
    /// <param name="passwordHash">O hash seguro da senha.</param>
    /// <param name="tenantId">ID do Tenant de pertença.</param>
    /// <param name="role">O papel de segurança atribuído.</param>
    /// <returns>Resultado contendo a instância do User.</returns>
    public static Result<User> Create(string email, string passwordHash, Guid tenantId, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email.Trim()))
        {
            return Result<User>.Failure(new Error("User.InvalidEmail", "O endereço de e-mail fornecido é inválido."));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result<User>.Failure(new Error("User.InvalidPassword", "A senha não pode ser nula ou vazia."));
        }

        if (tenantId == Guid.Empty)
        {
            return Result<User>.Failure(new Error("User.InvalidTenant", "O usuário deve ser vinculado a um Tenant válido."));
        }

        if (string.IsNullOrWhiteSpace(role) || !ValidRoles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return Result<User>.Failure(new Error("User.InvalidRole", "O papel de segurança atribuído ao usuário é inválido."));
        }

        // Normaliza a role para a capitalização oficial
        var normalizedRole = ValidRoles.First(r => r.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            TenantId = tenantId,
            Role = normalizedRole,
            IsMfaEnabled = false,
            MfaSecret = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        return Result<User>.Success(user);
    }

    /// <summary>
    /// Altera a role de segurança do usuário.
    /// </summary>
    /// <param name="newRole">O novo papel a ser atribuído.</param>
    /// <returns>Resultado indicando sucesso ou erro.</returns>
    public Result ChangeRole(string newRole)
    {
        if (string.IsNullOrWhiteSpace(newRole) || !ValidRoles.Contains(newRole.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(new Error("User.InvalidRole", "O papel especificado é inválido."));
        }

        Role = ValidRoles.First(r => r.Equals(newRole.Trim(), StringComparison.OrdinalIgnoreCase));
        return Result.Success();
    }

    /// <summary>
    /// Habilita o MFA TOTP com o segredo fornecido.
    /// </summary>
    /// <param name="secret">O segredo gerado.</param>
    /// <returns>Resultado de sucesso ou erro.</returns>
    public Result EnableMfa(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result.Failure(new Error("User.InvalidMfaSecret", "O segredo do MFA não pode ser vazio."));
        }

        MfaSecret = secret.Trim();
        IsMfaEnabled = true;
        return Result.Success();
    }

    /// <summary>
    /// Desabilita o MFA e limpa o segredo.
    /// </summary>
    /// <returns>Resultado de sucesso.</returns>
    public Result DisableMfa()
    {
        MfaSecret = string.Empty;
        IsMfaEnabled = false;
        return Result.Success();
    }
}
