using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Ports;
using Identity.Domain.Aggregates;
using Identity.Domain.Common;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext específico para o módulo Identity.
/// </summary>
public class IdentityDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// O ID do Tenant atual resolvido pelo provedor. Utilizado na expressão do filtro global de consulta.
    /// </summary>
    public Guid CurrentTenantId => _tenantProvider.TenantId;

    public DbSet<User> Users => Set<User>();

    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeamento do Agregado User
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique(); // Email é único no banco
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(u => u.Role).IsRequired().HasMaxLength(50);
            builder.Property(u => u.IsMfaEnabled).IsRequired();
            builder.Property(u => u.MfaSecret).HasMaxLength(100);
            builder.Property(u => u.CreatedAt).IsRequired();
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
