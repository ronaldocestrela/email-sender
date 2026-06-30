using System;
using System.Text.Json.Serialization;

namespace Gateway.Blazor.Models;

/// <summary>
/// Representa o resultado de uma chamada de API (Result Pattern no cliente).
/// </summary>
public class ClientResult
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("isFailure")]
    public bool IsFailure { get; set; }

    [JsonPropertyName("error")]
    public ClientError? Error { get; set; }

    public static ClientResult Success() => new() { IsSuccess = true, IsFailure = false };
    public static ClientResult Failure(ClientError error) => new() { IsSuccess = false, IsFailure = true, Error = error };
}

/// <summary>
/// Representa o resultado tipado de uma chamada de API (Result Pattern no cliente).
/// </summary>
public class ClientResult<TValue> : ClientResult
{
    [JsonPropertyName("value")]
    public TValue? Value { get; set; }

    public static ClientResult<TValue> Success(TValue value) => new() { IsSuccess = true, IsFailure = false, Value = value };
    public static new ClientResult<TValue> Failure(ClientError error) => new() { IsSuccess = false, IsFailure = true, Error = error };
}

/// <summary>
/// Detalhes de um erro retornado pelo Result Pattern da API.
/// </summary>
public class ClientError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Payload para requisição de login.
/// </summary>
public record LoginRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("mfaCode")] string? MfaCode = null
);

/// <summary>
/// Resposta obtida no login.
/// </summary>
public record LoginResponse(
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("requiresMfa")] bool RequiresMfa
);

/// <summary>
/// Configuração de setup inicial do MFA.
/// </summary>
public record MfaSetupResponse(
    [property: JsonPropertyName("secret")] string Secret,
    [property: JsonPropertyName("qrCodeUri")] string QrCodeUri
);

/// <summary>
/// Payload para confirmar o MFA.
/// </summary>
public record ConfirmMfaRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("secret")] string Secret
);

/// <summary>
/// Payload para criação de Tenant.
/// </summary>
public record CreateTenantRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mainDomain")] string MainDomain
);

/// <summary>
/// Informações básicas do Tenant retornado.
/// </summary>
public record TenantResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mainDomain")] string MainDomain,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt
);

/// <summary>
/// Resposta de chave de API gerada.
/// </summary>
public record ApiKeyResponse(
    [property: JsonPropertyName("plainTextKey")] string PlainTextKey,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt
);

/// <summary>
/// Detalhes de uma chave de API recuperada.
/// </summary>
public record ApiKeyDetailsResponse(
    [property: JsonPropertyName("keyHash")] string KeyHash,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("isRevoked")] bool IsRevoked
);

/// <summary>
/// Detalhes de um log histórico de e-mail disparado.
/// </summary>
public record EmailHistoryResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("senderDomain")] string SenderDomain,
    [property: JsonPropertyName("sentAt")] DateTime SentAt,
    [property: JsonPropertyName("isSuccess")] bool IsSuccess,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage
);

/// <summary>
/// Payload para geração de chaves de API.
/// </summary>
public record GenerateApiKeyRequest(
    [property: JsonPropertyName("description")] string Description
);

/// <summary>
/// Payload para solicitação de envio de e-mail.
/// </summary>
public record SendEmailRequest(
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("senderDomain")] string SenderDomain,
    [property: JsonPropertyName("templateVariables")] Dictionary<string, string> TemplateVariables
);
