using System;
using System.Collections.Generic;
using System.Linq;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.Entities;
using TenantManagement.Domain.ValueObjects;

namespace TenantManagement.Domain.Aggregates;

/// <summary>
/// Raiz do Agregado Tenant (Inquilino).
/// </summary>
public class Tenant
{
    /// <summary>
    /// Identificador único do Tenant.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nome corporativo do Tenant.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se o Tenant está ativo para operações de envio.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Data de criação do Tenant.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Domínios autorizados associados a este Tenant.
    /// </summary>
    public IReadOnlyCollection<DomainName> LinkedDomains => _linkedDomains.AsReadOnly();
    private readonly List<DomainName> _linkedDomains = new();

    /// <summary>
    /// Chaves de API registradas para este Tenant.
    /// </summary>
    public IReadOnlyCollection<ApiKey> ApiKeys => _apiKeys.AsReadOnly();
    private readonly List<ApiKey> _apiKeys = new();

    private Tenant() { }

    /// <summary>
    /// Cria um novo Tenant com configurações padrão de ativação.
    /// </summary>
    /// <param name="name">Nome corporativo do Tenant.</param>
    /// <returns>Um resultado contendo a instância de Tenant.</returns>
    public static Result<Tenant> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Tenant>.Failure(new Error("Tenant.InvalidName", "O nome do Tenant não pode ser vazio."));
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Tenant>.Success(tenant);
    }

    /// <summary>
    /// Adiciona um domínio verificado ao Tenant, garantindo que não seja duplicado.
    /// </summary>
    /// <param name="domain">O Value Object contendo o nome do domínio.</param>
    /// <returns>Resultado indicando sucesso ou falha na inclusão.</returns>
    public Result AddDomain(DomainName domain)
    {
        if (domain == null)
        {
            return Result.Failure(Error.NullValue);
        }

        if (_linkedDomains.Any(d => d.Value.Equals(domain.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(new Error("Tenant.DuplicateDomain", "Este domínio já está associado a este Tenant."));
        }

        _linkedDomains.Add(domain);
        return Result.Success();
    }

    /// <summary>
    /// Remove um domínio associado se ele existir.
    /// </summary>
    /// <param name="domain">O Value Object contendo o nome do domínio.</param>
    /// <returns>Resultado indicando sucesso ou falha na remoção.</returns>
    public Result RemoveDomain(DomainName domain)
    {
        if (domain == null)
        {
            return Result.Failure(Error.NullValue);
        }

        var existingDomain = _linkedDomains.FirstOrDefault(d => d.Value.Equals(domain.Value, StringComparison.OrdinalIgnoreCase));
        if (existingDomain == null)
        {
            return Result.Failure(new Error("Tenant.DomainNotFound", "O domínio especificado não foi encontrado neste Tenant."));
        }

        _linkedDomains.Remove(existingDomain);
        return Result.Success();
    }

    /// <summary>
    /// Gera uma nova chave de API segura e vincula a este Tenant.
    /// </summary>
    /// <param name="description">Uma descrição amigável para a finalidade da chave.</param>
    /// <returns>Resultado contendo a chave gerada.</returns>
    public Result<ApiKey> GenerateApiKey(string description)
    {
        var apiKeyResult = ApiKey.Create(description);
        if (apiKeyResult.IsFailure)
        {
            return apiKeyResult;
        }

        _apiKeys.Add(apiKeyResult.Value);
        return Result<ApiKey>.Success(apiKeyResult.Value);
    }

    /// <summary>
    /// Revoga uma chave de API existente do Tenant com base no hash fornecido.
    /// </summary>
    /// <param name="keyHash">O hash SHA256 da chave de API.</param>
    /// <returns>Resultado de sucesso ou falha.</returns>
    public Result RevokeApiKey(string keyHash)
    {
        if (string.IsNullOrWhiteSpace(keyHash))
        {
            return Result.Failure(new Error("Tenant.InvalidHash", "O hash da chave de API não pode ser nulo ou vazio."));
        }

        var apiKey = _apiKeys.FirstOrDefault(k => k.KeyHash.Equals(keyHash, StringComparison.OrdinalIgnoreCase));
        if (apiKey == null)
        {
            return Result.Failure(new Error("Tenant.ApiKeyNotFound", "Chave de API não encontrada neste Tenant."));
        }

        apiKey.Revoke();
        return Result.Success();
    }

    /// <summary>
    /// Ativa o Tenant para que possa efetuar envios de e-mails.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Desativa o Tenant temporariamente, bloqueando envios.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
