using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.Ports;

namespace TenantManagement.Application.UseCases;

/// <summary>
/// DTO de resposta contendo os detalhes de uma chave de API (sem expor o hash original diretamente se não desejado, ou para revogação).
/// </summary>
public record ApiKeyDetailsResponse(
    string KeyHash,
    string Description,
    DateTime CreatedAt,
    bool IsRevoked
);

/// <summary>
/// Interface para listar todos os Tenants.
/// </summary>
public interface IGetTenantsUseCase
{
    Task<Result<List<TenantResponse>>> ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface para listar as chaves de API de um Tenant.
/// </summary>
public interface IGetApiKeysUseCase
{
    Task<Result<List<ApiKeyDetailsResponse>>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface para revogar uma chave de API.
/// </summary>
public interface IRevokeApiKeyUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, string keyHash, CancellationToken cancellationToken = default);
}

/// <summary>
/// Caso de uso para obter todos os Tenants (Administrativo).
/// </summary>
public class GetTenantsUseCase : IGetTenantsUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantsUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<List<TenantResponse>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        
        var response = tenants.Select(t => new TenantResponse(
            t.Id,
            t.Name,
            t.LinkedDomains.FirstOrDefault()?.Value ?? string.Empty,
            t.IsActive,
            t.CreatedAt
        )).ToList();

        return Result<List<TenantResponse>>.Success(response);
    }
}

/// <summary>
/// Caso de uso para obter as chaves de API do Tenant logado.
/// </summary>
public class GetApiKeysUseCase : IGetApiKeysUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public GetApiKeysUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<List<ApiKeyDetailsResponse>>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<List<ApiKeyDetailsResponse>>.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var response = tenant.ApiKeys.Select(k => new ApiKeyDetailsResponse(
            k.KeyHash,
            k.Description,
            k.CreatedAt,
            k.IsRevoked
        )).ToList();

        return Result<List<ApiKeyDetailsResponse>>.Success(response);
    }
}

/// <summary>
/// Caso de uso para revogar uma chave de API do Tenant.
/// </summary>
public class RevokeApiKeyUseCase : IRevokeApiKeyUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public RevokeApiKeyUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result> ExecuteAsync(Guid tenantId, string keyHash, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var result = tenant.RevokeApiKey(keyHash);
        if (result.IsFailure)
        {
            return result;
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        return Result.Success();
    }
}
