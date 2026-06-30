using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Aggregates;
using EmailEngine.Domain.Entities;
using EmailEngine.Domain.Common;

namespace EmailEngine.Infrastructure.Persistence;

/// <summary>
/// DbContext específico para o módulo EmailEngine, lidando com o log de histórico e chaves/credenciais do provedor.
/// </summary>
public class EmailEngineDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// O ID do Tenant atual resolvido pelo provedor. Utilizado na expressão do filtro global de consulta.
    /// </summary>
    public Guid CurrentTenantId => _tenantProvider.TenantId;

    public DbSet<EmailHistory> EmailHistories => Set<EmailHistory>();
    public DbSet<EmailProviderSettings> EmailProviderSettings => Set<EmailProviderSettings>();

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

        // Mapeamento do Histórico de E-mails
        modelBuilder.Entity<EmailHistory>(builder =>
        {
            builder.ToTable("EmailHistories");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.To).IsRequired().HasMaxLength(255);
            builder.Property(h => h.Subject).IsRequired().HasMaxLength(300);
            builder.Property(h => h.Body).IsRequired();
            builder.Property(h => h.SenderDomain).IsRequired().HasMaxLength(150);
            builder.Property(h => h.SentAt).IsRequired();
            builder.Property(h => h.IsSuccess).IsRequired();
            builder.Property(h => h.ErrorMessage).HasMaxLength(500);
        });

        // Mapeamento de Configurações de Provedores
        modelBuilder.Entity<EmailProviderSettings>(builder =>
        {
            builder.ToTable("EmailProviderSettings");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Type).IsRequired().HasConversion<string>().HasMaxLength(50);
            builder.Property(p => p.ApiKey).HasMaxLength(500);
            builder.Property(p => p.SmtpHost).HasMaxLength(255);
            builder.Property(p => p.SmtpUsername).HasMaxLength(255);
            builder.Property(p => p.SmtpPassword).HasMaxLength(255);
            builder.Property(p => p.SenderAddress).IsRequired().HasMaxLength(255);
            builder.Property(p => p.SenderName).HasMaxLength(255);
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
    /// Sobrescreve o salvamento para auto-popular o TenantId das entidades adicionadas.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = CurrentTenantId;
                    }
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
