using System;
using System.Security.Cryptography;
using System.Text;
using TenantManagement.Domain.Common;

namespace TenantManagement.Domain.Entities;

/// <summary>
/// Representa uma chave de API vinculada a um Tenant para autenticação e autorização externa.
/// </summary>
public class ApiKey
{
    /// <summary>
    /// O hash SHA256 da chave de API salvo no banco de dados.
    /// </summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// A chave em texto limpo (apenas disponível em memória durante a criação).
    /// </summary>
    public string PlainTextKey { get; private set; } = string.Empty;

    /// <summary>
    /// A descrição da chave de API (ex: "Integração Hubspot").
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Data de criação da chave de API.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Indica se a chave de API foi revogada.
    /// </summary>
    public bool IsRevoked { get; private set; }

    private ApiKey() { }

    /// <summary>
    /// Cria uma nova chave de API com geração segura de tokens e hash correspondente.
    /// </summary>
    /// <param name="description">Uma descrição amigável para a finalidade da chave.</param>
    /// <returns>Um resultado contendo a instância de ApiKey.</returns>
    public static Result<ApiKey> Create(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Result<ApiKey>.Failure(new Error("ApiKey.InvalidDescription", "A descrição da chave de API não pode ser vazia."));
        }

        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // Gera uma string Base64 limpa sem caracteres especiais complexos para headers
        var base64Key = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        var plainTextKey = $"es_live_{base64Key}";

        // Calcula o hash SHA256 para persistência segura
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainTextKey));
        var keyHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var apiKey = new ApiKey
        {
            PlainTextKey = plainTextKey,
            KeyHash = keyHash,
            Description = description.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        return Result<ApiKey>.Success(apiKey);
    }

    /// <summary>
    /// Revoga o acesso desta chave de API imediatamente.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }
}
