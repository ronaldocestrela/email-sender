using System;

namespace Identity.Application.Ports;

/// <summary>
/// Port de entrada/saída para prover o ID do Tenant da requisição atual.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// O identificador único do Tenant resolvido contextualmente.
    /// </summary>
    Guid TenantId { get; }
}
