using System;

namespace EmailEngine.Application.Ports;

/// <summary>
/// Port de saída/entrada para definir o TenantId da thread atual (especialmente em fluxos assíncronos de mensageria).
/// </summary>
public interface ITenantSetter
{
    /// <summary>
    /// Atribui o TenantId para o contexto assíncrono corrente.
    /// </summary>
    void SetTenantId(Guid tenantId);
}
