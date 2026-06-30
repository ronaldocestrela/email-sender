using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Domain.Ports;

namespace TenantManagement.Application.UseCases;

/// <summary>
/// Interface do caso de uso para cadastro de novos Tenants.
/// </summary>
public interface ICreateTenantUseCase
{
    /// <summary>
    /// Executa o cadastro de um novo Tenant com seu domínio principal.
    /// </summary>
    Task<Result<TenantResponse>> ExecuteAsync(string name, string mainDomain, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do caso de uso de criação de Tenant.
/// </summary>
public class CreateTenantUseCase : ICreateTenantUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// Registra o Tenant e salva no banco de dados.
    /// </summary>
    public async Task<Result<TenantResponse>> ExecuteAsync(string name, string mainDomain, CancellationToken cancellationToken = default)
    {
        var domainResult = TenantManagement.Domain.ValueObjects.DomainName.Create(mainDomain);
        if (domainResult.IsFailure)
        {
            return Result<TenantResponse>.Failure(domainResult.Error);
        }

        var result = Tenant.Create(name);
        if (result.IsFailure)
        {
            return Result<TenantResponse>.Failure(result.Error);
        }

        var tenant = result.Value;
        var addDomainResult = tenant.AddDomain(domainResult.Value);
        if (addDomainResult.IsFailure)
        {
            return Result<TenantResponse>.Failure(addDomainResult.Error);
        }

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        // Retorna a resposta contendo os detalhes cadastrados
        var response = new TenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.LinkedDomains.FirstOrDefault()?.Value ?? string.Empty,
            tenant.IsActive,
            tenant.CreatedAt);

        return Result<TenantResponse>.Success(response);
    }
}
