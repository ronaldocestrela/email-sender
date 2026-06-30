using System.Text.Json;

namespace Gateway.Blazor.Helpers;

/// <summary>
/// Utilitário para extrair mensagens de erro legíveis de respostas HTTP da API.
/// </summary>
public static class ApiResponseHelper
{
    /// <summary>
    /// Regex de validação de domínio alinhada ao Value Object DomainName do backend.
    /// </summary>
    public const string DomainValidationPattern =
        @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$";

    /// <summary>
    /// Lê o corpo de uma resposta HTTP de erro e retorna a mensagem mais relevante.
    /// </summary>
    public static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return fallback;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var messageProp))
            {
                return messageProp.GetString() ?? fallback;
            }

            if (root.TryGetProperty("error", out var errorProp))
            {
                if (errorProp.ValueKind == JsonValueKind.Object &&
                    errorProp.TryGetProperty("message", out var nestedMessage))
                {
                    return nestedMessage.GetString() ?? fallback;
                }

                if (errorProp.ValueKind == JsonValueKind.String)
                {
                    return errorProp.GetString() ?? fallback;
                }
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString() ?? fallback;
            }
        }
        catch (JsonException)
        {
            return content.Trim('"');
        }

        return fallback;
    }
}
