using System;

namespace TenantManagement.Domain.Common;

/// <summary>
/// Define que a entidade pertence a um Tenant específico para isolamento lógico.
/// </summary>
public interface IMustHaveTenant
{
    /// <summary>
    /// O identificador único do Tenant associado.
    /// </summary>
    public Guid TenantId { get; set; }
}
