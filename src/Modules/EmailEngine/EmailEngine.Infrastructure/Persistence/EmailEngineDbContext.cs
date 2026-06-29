using System;
using Microsoft.EntityFrameworkCore;
using EmailEngine.Application.Ports;

namespace EmailEngine.Infrastructure.Persistence;

/// <summary>
/// DbContext específico para o módulo EmailEngine.
/// </summary>
public class EmailEngineDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// O ID do Tenant atual resolvido pelo provedor. Utilizado na expressão do filtro global de consulta.
    /// </summary>
    public Guid CurrentTenantId => _tenantProvider.TenantId;

    public EmailEngineDbContext(
        DbContextOptions<EmailEngineDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // As entidades específicas de e-mail serão mapeadas aqui na Fase 4.
    }
}
