using System;

namespace EmailEngine.Domain.Common;

/// <summary>
/// Define que a entidade pertence a um Tenant específico no módulo EmailEngine.
/// </summary>
public interface IMustHaveTenant
{
    /// <summary>
    /// O identificador único do Tenant associado.
    /// </summary>
    public Guid TenantId { get; set; }
}
