using System;
using System.Threading;

namespace Gateway.Bootstrapper.Contexts;

/// <summary>
/// Contexto seguro para gerenciamento do escopo do Tenant de forma thread-safe por fluxo assíncrono.
/// Implementa a interface de ITenantProvider de todos os módulos.
/// </summary>
public class TenantContext : 
    TenantManagement.Application.Ports.ITenantProvider,
    Identity.Application.Ports.ITenantProvider,
    EmailEngine.Application.Ports.ITenantProvider
{
    private static readonly AsyncLocal<Guid> _currentTenantId = new();

    /// <summary>
    /// O identificador único do Tenant associado ao contexto assíncrono ou de requisição atual.
    /// </summary>
    public Guid TenantId => _currentTenantId.Value;

    /// <summary>
    /// Define estaticamente o ID do Tenant no contexto do fluxo assíncrono atual.
    /// </summary>
    /// <param name="tenantId">O ID do Tenant resolvido.</param>
    public static void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId.Value = tenantId;
    }

    /// <summary>
    /// Define de forma de instância o ID do Tenant para injeção via DI.
    /// </summary>
    /// <param name="tenantId">O ID do Tenant resolvido.</param>
    public void SetTenantId(Guid tenantId)
    {
        _currentTenantId.Value = tenantId;
    }
}
