using System;
using System.Threading;
using System.Threading.Tasks;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.Ports;

namespace TenantManagement.Application.UseCases;

/// <summary>
/// Interface do caso de uso para geração de novas chaves de API.
/// </summary>
public interface IGenerateApiKeyUseCase
{
    /// <summary>
    /// Gera uma nova chave de API para o Tenant informado.
    /// </summary>
    Task<Result<ApiKeyResponse>> ExecuteAsync(Guid tenantId, string description, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do caso de uso para geração de chaves de API.
/// </summary>
public class GenerateApiKeyUseCase : IGenerateApiKeyUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public GenerateApiKeyUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// Recupera o Tenant, gera a chave de API (salvando o hash) e retorna a chave em texto plano.
    /// </summary>
    public async Task<Result<ApiKeyResponse>> ExecuteAsync(Guid tenantId, string description, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<ApiKeyResponse>.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var keyResult = tenant.GenerateApiKey(description);
        if (keyResult.IsFailure)
        {
            return Result<ApiKeyResponse>.Failure(keyResult.Error);
        }

        var apiKey = keyResult.Value;
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        // Retorna a resposta contendo a chave em texto claro (PlainTextKey)
        var response = new ApiKeyResponse(
            apiKey.PlainTextKey,
            apiKey.Description,
            apiKey.CreatedAt);

        return Result<ApiKeyResponse>.Success(response);
    }
}
