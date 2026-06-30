using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmailEngine.Application.Ports;
using EmailEngine.Application.UseCases;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Contracts;
using MassTransit;

namespace Gateway.Bootstrapper.Controllers;

/// <summary>
/// Controlador responsável pelas solicitações assíncronas de disparo de e-mails e consulta de histórico.
/// </summary>
[Route("api/emails")]
public class EmailController : ApiControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IGetEmailHistoryUseCase _getEmailHistoryUseCase;

    public EmailController(
        ITenantProvider tenantProvider,
        IPublishEndpoint publishEndpoint,
        IGetEmailHistoryUseCase getEmailHistoryUseCase)
    {
        _tenantProvider = tenantProvider;
        _publishEndpoint = publishEndpoint;
        _getEmailHistoryUseCase = getEmailHistoryUseCase;
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

    /// <summary>
    /// Recupera a lista histórica de e-mails disparados correspondentes ao Tenant autenticado.
    /// </summary>
    [Authorize]
    [HttpGet("history")]
    public async Task<ActionResult> History(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized("Inquilino não identificado no contexto da requisição.");
        }

        var result = await _getEmailHistoryUseCase.ExecuteAsync(cancellationToken);
        return HandleResult(result);
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
