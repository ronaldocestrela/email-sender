using System;
using System.Text.RegularExpressions;
using TenantManagement.Domain.Common;

namespace TenantManagement.Domain.ValueObjects;

/// <summary>
/// Representa o Value Object de um nome de domínio verificado.
/// </summary>
public record DomainName
{
    private static readonly Regex DomainRegex = new(
        @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// O valor do domínio em formato texto.
    /// </summary>
    public string Value { get; }

    private DomainName(string value)
    {
        Value = value.ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Cria uma nova instância de DomainName validando o formato do domínio.
    /// </summary>
    /// <param name="domain">O nome do domínio a ser validado.</param>
    /// <returns>Um resultado contendo a instância de DomainName ou um erro.</returns>
    public static Result<DomainName> Create(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return Result<DomainName>.Failure(new Error("DomainName.Invalid", "O nome do domínio não pode ser vazio."));
        }

        var normalizedDomain = domain.Trim();

        if (!DomainRegex.IsMatch(normalizedDomain))
        {
            return Result<DomainName>.Failure(new Error("DomainName.Invalid", "O formato de domínio fornecido é inválido."));
        }

        return Result<DomainName>.Success(new DomainName(normalizedDomain));
    }

    /// <summary>
    /// Representação textual do domínio.
    /// </summary>
    public override string ToString() => Value;
}
