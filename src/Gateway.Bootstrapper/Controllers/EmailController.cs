using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Contracts;
using MassTransit;

namespace Gateway.Bootstrapper.Controllers;

/// <summary>
/// Controlador responsável pelas solicitações assíncronas de disparo de e-mails.
/// </summary>
[Route("api/emails")]
public class EmailController : ApiControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IPublishEndpoint _publishEndpoint;

    public EmailController(
        ITenantProvider tenantProvider,
        IPublishEndpoint publishEndpoint)
    {
        _tenantProvider = tenantProvider;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Envia uma solicitação de disparo assíncrono de e-mail para a fila de mensageria.
    /// Exige autenticação prévia via cabeçalho X-API-KEY.
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult> Send([FromBody] SendEmailRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Corpo da requisição inválido.");
        }

        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
        {
            var errorResult = Result.Failure(new Error("Email.UnauthorizedTenant", "Chave de API inválida ou ausente."));
            return HandleResult(errorResult);
        }

        // Constrói o comando imutável do MassTransit
        var command = new SendEmailCommand(
            tenantId,
            request.To,
            request.Subject,
            request.Body,
            request.SenderDomain,
            request.TemplateVariables);

        // Publica na fila do RabbitMQ
        await _publishEndpoint.Publish(command, cancellationToken);

        // Retorna sucesso padronizado imediato (202 Accepted)
        var successResult = Result.Success();
        return Accepted(successResult);
    }
}

/// <summary>
/// Parâmetros de solicitação de envio assíncrono de e-mail.
/// </summary>
public record SendEmailRequest(
    string To,
    string Subject,
    string Body,
    string SenderDomain,
    Dictionary<string, string> TemplateVariables
);
