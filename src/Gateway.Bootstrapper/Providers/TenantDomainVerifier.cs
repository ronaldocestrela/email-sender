using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Application.Ports;
using TenantManagement.Domain.Ports;

namespace Gateway.Bootstrapper.Providers;

/// <summary>
/// Provedor que integra os módulos de TenantManagement e EmailEngine para verificar domínios de inquilinos.
/// </summary>
public class TenantDomainVerifier : IEmailTenantDomainVerifier
{
    private readonly ITenantRepository _tenantRepository;

    public TenantDomainVerifier(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// Consulta o Tenant no banco de dados e verifica se o domínio está cadastrado e verificado.
    /// </summary>
    public async Task<bool> IsDomainVerifiedAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        var domainClean = domain.Trim().ToLowerInvariant();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        return tenant.LinkedDomains.Any(d => d.Value.Equals(domainClean, StringComparison.OrdinalIgnoreCase) && d.IsVerified);
    }
}
