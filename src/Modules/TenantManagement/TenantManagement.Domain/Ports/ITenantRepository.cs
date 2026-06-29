using System;
using System.Threading;
using System.Threading.Tasks;
using TenantManagement.Domain.Aggregates;

namespace TenantManagement.Domain.Ports;

/// <summary>
/// Port de saída para persistência e consulta do Agregado Tenant.
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Recupera um Tenant pelo seu identificador único.
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera um Tenant a partir do hash da chave de API.
    /// </summary>
    Task<Tenant?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo Tenant no banco de dados.
    /// </summary>
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o estado de um Tenant existente.
    /// </summary>
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
