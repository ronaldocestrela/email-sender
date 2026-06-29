using System;
using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Domain.Entities;

namespace EmailEngine.Application.Ports;

/// <summary>
/// Port de saída para recuperação de configurações personalizadas de e-mail por Tenant.
/// </summary>
public interface IEmailProviderSettingsRepository
{
    /// <summary>
    /// Recupera as configurações de provedor do Tenant ativo.
    /// </summary>
    Task<EmailProviderSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona ou atualiza as configurações de provedor de um Tenant.
    /// </summary>
    Task SaveAsync(EmailProviderSettings settings, CancellationToken cancellationToken = default);
}
