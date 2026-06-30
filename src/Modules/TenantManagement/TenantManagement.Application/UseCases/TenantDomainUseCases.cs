using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TenantManagement.Application.Ports;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.Ports;

namespace TenantManagement.Application.UseCases;

/// <summary>
/// Interface do caso de uso para adicionar um domínio ao Tenant.
/// </summary>
public interface IAddTenantDomainUseCase
{
    Task<Result<TenantDomainResponse>> ExecuteAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface do caso de uso para remover um domínio do Tenant.
/// </summary>
public interface IRemoveTenantDomainUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface do caso de uso para recuperar domínios do Tenant.
/// </summary>
public interface IGetTenantDomainsUseCase
{
    Task<Result<List<TenantDomainResponse>>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface do caso de uso para verificar propriedade do domínio via DNS TXT.
/// </summary>
public interface IVerifyTenantDomainUseCase
{
    Task<Result<TenantDomainResponse>> ExecuteAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do caso de uso para adicionar domínios.
/// </summary>
public class AddTenantDomainUseCase : IAddTenantDomainUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public AddTenantDomainUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantDomainResponse>> ExecuteAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<TenantDomainResponse>.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var result = tenant.AddDomain(domain, isVerified: false);
        if (result.IsFailure)
        {
            return Result<TenantDomainResponse>.Failure(result.Error);
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        var tenantDomain = tenant.LinkedDomains.First(d => d.Value.Equals(domain.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));

        var response = new TenantDomainResponse(
            tenantDomain.Id,
            tenantDomain.Value,
            tenantDomain.IsVerified,
            tenantDomain.VerificationToken,
            tenantDomain.VerifiedAt
        );

        return Result<TenantDomainResponse>.Success(response);
    }
}

/// <summary>
/// Implementação do caso de uso para remover domínios.
/// </summary>
public class RemoveTenantDomainUseCase : IRemoveTenantDomainUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public RemoveTenantDomainUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result> ExecuteAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var result = tenant.RemoveDomain(domain);
        if (result.IsFailure)
        {
            return result;
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// Implementação do caso de uso para listar domínios.
/// </summary>
public class GetTenantDomainsUseCase : IGetTenantDomainsUseCase
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantDomainsUseCase(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<List<TenantDomainResponse>>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<List<TenantDomainResponse>>.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var responseList = tenant.LinkedDomains
            .Select(d => new TenantDomainResponse(d.Id, d.Value, d.IsVerified, d.VerificationToken, d.VerifiedAt))
            .ToList();

        return Result<List<TenantDomainResponse>>.Success(responseList);
    }
}

/// <summary>
/// Implementação do caso de uso para verificar domínios via DNS.
/// </summary>
public class VerifyTenantDomainUseCase : IVerifyTenantDomainUseCase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IDnsService _dnsService;

    public VerifyTenantDomainUseCase(ITenantRepository tenantRepository, IDnsService dnsService)
    {
        _tenantRepository = tenantRepository;
        _dnsService = dnsService;
    }

    public async Task<Result<TenantDomainResponse>> ExecuteAsync(Guid tenantId, string domain, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return Result<TenantDomainResponse>.Failure(new Error("Tenant.NotFound", "Inquilino não encontrado."));
        }

        var domainClean = domain.Trim().ToLowerInvariant();
        var tenantDomain = tenant.LinkedDomains.FirstOrDefault(d => d.Value.Equals(domainClean, StringComparison.OrdinalIgnoreCase));
        if (tenantDomain == null)
        {
            return Result<TenantDomainResponse>.Failure(new Error("Tenant.DomainNotFound", "O domínio especificado não foi encontrado neste Tenant."));
        }

        if (tenantDomain.IsVerified)
        {
            return Result<TenantDomainResponse>.Success(new TenantDomainResponse(
                tenantDomain.Id,
                tenantDomain.Value,
                tenantDomain.IsVerified,
                tenantDomain.VerificationToken,
                tenantDomain.VerifiedAt
            ));
        }

        // Executa a consulta DNS TXT
        var isDnsValid = await _dnsService.VerifyTxtRecordAsync(tenantDomain.Value, tenantDomain.VerificationToken, cancellationToken);
        if (!isDnsValid)
        {
            return Result<TenantDomainResponse>.Failure(new Error("Tenant.DomainVerificationFailed", 
                "A verificação do DNS falhou. Certifique-se de que o registro TXT foi adicionado e propagado."));
        }

        // Altera status de verificação no agregado
        var verifyResult = tenant.VerifyDomain(tenantDomain.Value);
        if (verifyResult.IsFailure)
        {
            return Result<TenantDomainResponse>.Failure(verifyResult.Error);
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        var response = new TenantDomainResponse(
            tenantDomain.Id,
            tenantDomain.Value,
            tenantDomain.IsVerified,
            tenantDomain.VerificationToken,
            tenantDomain.VerifiedAt
        );

        return Result<TenantDomainResponse>.Success(response);
    }
}
