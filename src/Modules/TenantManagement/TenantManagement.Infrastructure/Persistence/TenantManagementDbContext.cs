using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TenantManagement.Application.Ports;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.Entities;
using TenantManagement.Domain.ValueObjects;

namespace TenantManagement.Infrastructure.Persistence;

/// <summary>
/// DbContext específico para o módulo TenantManagement.
/// </summary>
public class TenantManagementDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// O ID do Tenant atual resolvido pelo provedor. Utilizado na expressão do filtro global de consulta.
    /// </summary>
    public Guid CurrentTenantId => _tenantProvider.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public TenantManagementDbContext(
        DbContextOptions<TenantManagementDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeamento do Agregado Tenant
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("Tenants");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(150);
            builder.Property(t => t.IsActive).IsRequired();
            builder.Property(t => t.CreatedAt).IsRequired();

            // Configura o filtro global para a raiz do Agregado Tenant
            builder.HasQueryFilter(t => t.Id == CurrentTenantId);

            // Mapeamento da coleção de TenantDomains (Owned type)
            builder.OwnsMany(t => t.LinkedDomains, d =>
            {
                d.ToTable("TenantDomains");
                d.WithOwner().HasForeignKey("TenantId");
                d.Property<Guid>("Id");
                d.HasKey("Id");
                d.Property(x => x.Value).HasColumnName("Domain").IsRequired().HasMaxLength(255);
                d.Property(x => x.IsVerified).HasColumnName("IsVerified").IsRequired().HasDefaultValue(false);
                d.Property(x => x.VerificationToken).HasColumnName("VerificationToken").IsRequired().HasMaxLength(100);
                d.Property(x => x.VerifiedAt).HasColumnName("VerifiedAt");
            });

            // Mapeamento da coleção de ApiKeys (Owned type)
            builder.OwnsMany(t => t.ApiKeys, k =>
            {
                k.ToTable("TenantApiKeys");
                k.WithOwner().HasForeignKey("TenantId");
                k.Property<Guid>("Id");
                k.HasKey("Id");
                k.Property(x => x.KeyHash).IsRequired().HasMaxLength(64);
                k.Property(x => x.Description).IsRequired().HasMaxLength(200);
                k.Property(x => x.CreatedAt).IsRequired();
                k.Property(x => x.IsRevoked).IsRequired();
                k.Ignore(x => x.PlainTextKey);
            });

            // Backing fields privados — configurados após OwnsMany para rastrear mutações do agregado
            builder.Navigation(t => t.LinkedDomains)
                .HasField("_linkedDomains")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(t => t.ApiKeys)
                .HasField("_apiKeys")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Configuração dinâmica para todas as entidades IMustHaveTenant
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateTenantFilterExpression(entityType.ClrType));
            }
        }
    }

    /// <summary>
    /// Sobrescreve o salvamento de alterações para garantir que novas entidades herdem o TenantId atual.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = CurrentTenantId;
                    break;
                case EntityState.Modified:
                    entry.Property(x => x.TenantId).IsModified = false;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private LambdaExpression CreateTenantFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "x");
        var property = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
        var dbContextConstant = Expression.Constant(this);
        var tenantIdProperty = Expression.Property(dbContextConstant, nameof(CurrentTenantId));
        var body = Expression.Equal(property, tenantIdProperty);
        return Expression.Lambda(body, parameter);
    }
}
