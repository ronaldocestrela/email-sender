using Identity.Domain.Aggregates;

namespace Identity.Application.Ports;

/// <summary>
/// Port de saída para geração de Tokens JWT.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Gera um Token JWT para o usuário contendo as Claims de TenantId e Role.
    /// </summary>
    /// <param name="user">O usuário para o qual gerar o token.</param>
    /// <returns>O JWT em formato string.</returns>
    string GenerateToken(User user);
}
