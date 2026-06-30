using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Domain.Entities;
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
        return await _context.Tenants
            .Include(t => t.LinkedDomains)
            .Include(t => t.ApiKeys)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
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
            .Include(t => t.LinkedDomains)
            .Include(t => t.ApiKeys)
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
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Atualiza o estado de um Tenant existente.
    /// Quando a entidade já está rastreada pelo contexto (fluxo típico após GetByIdAsync),
    /// persiste apenas as alterações detectadas pelo change tracker sem reanexar o grafo.
    /// </summary>
    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        var entry = _context.Entry(tenant);

        if (entry.State == EntityState.Detached)
        {
            _context.Tenants.Attach(tenant);
            entry.State = EntityState.Unchanged;
        }
        else if (entry.State == EntityState.Modified && !entry.Properties.Any(p => p.IsModified))
        {
            // Evita UPDATE desnecessário na raiz quando apenas coleções owned foram alteradas.
            entry.State = EntityState.Unchanged;
        }

        _context.ChangeTracker.DetectChanges();
        await NormalizeOwnedEntityStatesAsync(tenant, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Corrige owned entities recém-adicionadas ao agregado que o EF Core marca incorretamente como Modified.
    /// Isso ocorre quando coleções encapsuladas usam backing fields privados.
    /// </summary>
    private async Task NormalizeOwnedEntityStatesAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var modifiedOwnedEntries = _context.ChangeTracker.Entries()
            .Where(e => e.Metadata.IsOwned() && e.State == EntityState.Modified)
            .ToList();

        if (modifiedOwnedEntries.Count == 0)
        {
            return;
        }

        var existingIds = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenant.Id)
            .Select(t => new
            {
                DomainIds = t.LinkedDomains.Select(d => d.Id).ToList(),
                ApiKeyIds = t.ApiKeys.Select(k => EF.Property<Guid>(k, "Id")).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var existingDomainIds = existingIds?.DomainIds.ToHashSet() ?? new HashSet<Guid>();
        var existingApiKeyIds = existingIds?.ApiKeyIds.ToHashSet() ?? new HashSet<Guid>();

        foreach (var ownedEntry in modifiedOwnedEntries)
        {
            var idProperty = ownedEntry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            if (idProperty?.CurrentValue is not Guid entityId)
            {
                continue;
            }

            var existsInDatabase = ownedEntry.Metadata.ClrType.Name switch
            {
                nameof(TenantDomain) => existingDomainIds.Contains(entityId),
                nameof(ApiKey) => existingApiKeyIds.Contains(entityId),
                _ => true
            };

            if (!existsInDatabase)
            {
                ownedEntry.State = EntityState.Added;
            }
        }
    }

    /// <summary>
    /// Recupera todos os Tenants cadastrados no sistema.
    /// </summary>
    public async Task<System.Collections.Generic.List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.LinkedDomains)
            .Include(t => t.ApiKeys)
            .ToListAsync(cancellationToken);
    }
}
