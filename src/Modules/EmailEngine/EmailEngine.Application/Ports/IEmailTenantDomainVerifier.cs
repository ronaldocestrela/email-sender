using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmailEngine.Application.Ports;

/// <summary>
/// Port de saída para o EmailEngine consultar a verificação do domínio do inquilino.
/// </summary>
public interface IEmailTenantDomainVerifier
{
    /// <summary>
    /// Verifica se o domínio de envio informado está registrado e verificado para o Tenant.
    /// </summary>
    /// <param name="tenantId">O ID do Tenant.</param>
    /// <param name="domain">O domínio a ser verificado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se o domínio estiver registrado e verificado, caso contrário False.</returns>
    Task<bool> IsDomainVerifiedAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default);
}
