using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Domain.Ports;

namespace TenantManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório concreto para persistência e consulta do Agregado Tenant.
/// Implementa a interface Port ITenantRepository.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly TenantManagementDbContext _context;

    public TenantRepository(TenantManagementDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera um Tenant pelo seu identificador único.
    /// </summary>
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <summary>
    /// Recupera um Tenant a partir do hash da chave de API, ignorando filtros globais.
    /// </summary>
    public async Task<Tenant?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKeyHash))
        {
            return null;
        }

        return await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.ApiKeys.Any(k => k.KeyHash.Equals(apiKeyHash, StringComparison.OrdinalIgnoreCase) && !k.IsRevoked), cancellationToken);
    }

    /// <summary>
    /// Adiciona um novo Tenant no banco de dados.
    /// </summary>
    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        await _context.Tenants.AddAsync(tenant, cancellationToken);
    }

    /// <summary>
    /// Atualiza o estado de um Tenant existente.
    /// </summary>
    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        _context.Tenants.Update(tenant);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Recupera todos os Tenants cadastrados no sistema.
    /// </summary>
    public async Task<System.Collections.Generic.List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants.ToListAsync(cancellationToken);
    }
}
