using TenantManagement.Application.Ports;

namespace TenantManagement.Infrastructure.Tests.Support;

/// <summary>
/// Provedor de tenant fixo para testes de integração com DbContext.
/// </summary>
public sealed class TestTenantProvider : ITenantProvider
{
    public TestTenantProvider(Guid tenantId)
    {
        TenantId = tenantId;
    }

    /// <inheritdoc />
    public Guid TenantId { get; set; }
}
