using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Entities;

namespace EmailEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório concreto para persistência e recuperação das configurações de provedores de e-mail por Tenant.
/// </summary>
public class EmailProviderSettingsRepository : IEmailProviderSettingsRepository
{
    private readonly EmailEngineDbContext _context;

    public EmailProviderSettingsRepository(EmailEngineDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera as configurações do provedor de e-mail associadas ao TenantId especificado, de forma segura.
    /// </summary>
    public async Task<EmailProviderSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailProviderSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
    }

    /// <summary>
    /// Adiciona ou atualiza as configurações do provedor no banco de dados.
    /// </summary>
    public async Task SaveAsync(EmailProviderSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var exists = await _context.EmailProviderSettings
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == settings.Id, cancellationToken);

        if (exists)
        {
            _context.EmailProviderSettings.Update(settings);
        }
        else
        {
            await _context.EmailProviderSettings.AddAsync(settings, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
