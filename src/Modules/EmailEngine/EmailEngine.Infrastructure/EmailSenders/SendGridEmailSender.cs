using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Entities;
using EmailEngine.Domain.Enums;

namespace EmailEngine.Infrastructure.EmailSenders;

/// <summary>
/// Adapter concreto para envio físico de e-mails utilizando a API HTTP v3 do SendGrid.
/// </summary>
public class SendGridEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SendGridEmailSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Envia o e-mail realizando uma chamada POST autenticada para a API do SendGrid.
    /// </summary>
    public async Task<Result> SendAsync(
        string to,
        string subject,
        string body,
        string senderAddress,
        string senderName,
        EmailProviderSettings? customSettings = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Padrão do Sistema
            var apiKey = _configuration["SendGridSettings:ApiKey"] ?? string.Empty;

            // Sobrescreve se o Tenant possuir configurações customizadas do SendGrid
            if (customSettings != null && customSettings.Type == EmailProviderType.SendGrid)
            {
                apiKey = customSettings.ApiKey;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Result.Failure(new Error("SendGrid.MissingApiKey", "A chave de API do SendGrid não foi configurada."));
            }

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[]
                        {
                            new { email = to }
                        }
                    }
                },
                from = new
                {
                    email = senderAddress,
                    name = senderName
                },
                subject = subject,
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = body
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure(new Error("SendGrid.HttpError", $"SendGrid API retornou erro {response.StatusCode}: {errorContent}"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("SendGrid.SendFailed", $"Falha ao enviar e-mail via SendGrid: {ex.Message}"));
        }
    }
}
