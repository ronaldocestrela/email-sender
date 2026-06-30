using System;
using TenantManagement.Domain.Common;
using TenantManagement.Domain.ValueObjects;

namespace TenantManagement.Domain.Entities;

/// <summary>
/// Representa um domínio de envio associado a um Tenant e seu status de verificação DNS.
/// </summary>
public class TenantDomain
{
    /// <summary>
    /// Identificador único da associação do domínio.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// O valor do domínio em formato texto (ex: "acme.com").
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se o domínio já foi verificado via registros DNS.
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// Token gerado pelo sistema para o cliente cadastrar em seu DNS TXT.
    /// </summary>
    public string VerificationToken { get; private set; } = string.Empty;

    /// <summary>
    /// Data/Hora em que o domínio foi verificado.
    /// </summary>
    public DateTime? VerifiedAt { get; private set; }

    private TenantDomain() { }

    /// <summary>
    /// Cria uma nova associação de domínio para o Tenant, gerando um token de verificação.
    /// </summary>
    /// <param name="domain">O nome do domínio a ser validado e criado.</param>
    /// <param name="isVerified">Define se o domínio já deve nascer verificado (ex: para o domínio principal inicial).</param>
    /// <returns>Um resultado contendo o TenantDomain ou erro.</returns>
    public static Result<TenantDomain> Create(string domain, bool isVerified = false)
    {
        var domainResult = DomainName.Create(domain);
        if (domainResult.IsFailure)
        {
            return Result<TenantDomain>.Failure(domainResult.Error);
        }

        var tenantDomain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            Value = domainResult.Value.Value,
            IsVerified = isVerified,
            VerificationToken = $"email-sender-verification={Guid.NewGuid():N}",
            VerifiedAt = isVerified ? DateTime.UtcNow : null
        };

        return Result<TenantDomain>.Success(tenantDomain);
    }

    /// <summary>
    /// Marca o domínio como verificado pelo DNS.
    /// </summary>
    public void Verify()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
    }
}
