using System;

namespace TenantManagement.Application.UseCases;

/// <summary>
/// DTO de resposta contendo os detalhes cadastrais do Tenant.
/// </summary>
public record TenantResponse(
    Guid Id,
    string Name,
    string MainDomain,
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>
/// DTO de resposta contendo a chave de API recém-gerada em texto plano.
/// </summary>
public record ApiKeyResponse(
    string PlainTextKey,
    string Description,
    DateTime CreatedAt
);

/// <summary>
/// DTO de resposta contendo os detalhes de um domínio do Tenant.
/// </summary>
public record TenantDomainResponse(
    Guid Id,
    string Value,
    bool IsVerified,
    string VerificationToken,
    DateTime? VerifiedAt
);
