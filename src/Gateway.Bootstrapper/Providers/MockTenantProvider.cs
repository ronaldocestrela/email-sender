using System;

namespace Gateway.Bootstrapper.Providers;

/// <summary>
/// Provedor temporário de Tenant para fins de desenvolvimento inicial e migrações.
/// Implementa as interfaces de ITenantProvider de todos os módulos.
/// </summary>
public class MockTenantProvider : 
    TenantManagement.Application.Ports.ITenantProvider,
    Identity.Application.Ports.ITenantProvider,
    EmailEngine.Application.Ports.ITenantProvider
{
    /// <summary>
    /// ID fixo de inquilino de teste para a fase de setup inicial.
    /// </summary>
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001");
}
