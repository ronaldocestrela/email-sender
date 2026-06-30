using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantManagement.Application.Ports;
using TenantManagement.Application.UseCases;

namespace Gateway.Bootstrapper.Controllers;

/// <summary>
/// Controlador responsável pelas operações cadastrais do Tenant e chaves de acesso.
/// </summary>
[Route("api/tenants")]
public class TenantController : ApiControllerBase
{
    private readonly ICreateTenantUseCase _createTenantUseCase;
    private readonly IGenerateApiKeyUseCase _generateApiKeyUseCase;
    private readonly IGetTenantsUseCase _getTenantsUseCase;
    private readonly IGetApiKeysUseCase _getApiKeysUseCase;
    private readonly IRevokeApiKeyUseCase _revokeApiKeyUseCase;
    private readonly ITenantProvider _tenantProvider;

    public TenantController(
        ICreateTenantUseCase createTenantUseCase,
        IGenerateApiKeyUseCase generateApiKeyUseCase,
        IGetTenantsUseCase getTenantsUseCase,
        IGetApiKeysUseCase getApiKeysUseCase,
        IRevokeApiKeyUseCase revokeApiKeyUseCase,
        ITenantProvider tenantProvider)
    {
        _createTenantUseCase = createTenantUseCase;
        _generateApiKeyUseCase = generateApiKeyUseCase;
        _getTenantsUseCase = getTenantsUseCase;
        _getApiKeysUseCase = getApiKeysUseCase;
        _revokeApiKeyUseCase = revokeApiKeyUseCase;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Cadastra um novo inquilino (Tenant) no sistema.
    /// Apenas administradores gerais possuem acesso.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Corpo da requisição inválido.");
        }

        var result = await _createTenantUseCase.ExecuteAsync(request.Name, request.MainDomain, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lista todos os Tenants cadastrados.
    /// Apenas administradores gerais possuem acesso.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _getTenantsUseCase.ExecuteAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gera uma nova chave de acesso (API Key) em texto plano para o Tenant autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("api-keys")]
    public async Task<ActionResult> GenerateApiKey([FromBody] GenerateApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Corpo da requisição inválido.");
        }

        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized("Inquilino não identificado no contexto da requisição.");
        }

        var result = await _generateApiKeyUseCase.ExecuteAsync(tenantId, request.Description, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lista as chaves de API ativas e revogadas do Tenant autenticado.
    /// </summary>
    [Authorize]
    [HttpGet("api-keys")]
    public async Task<ActionResult> ListApiKeys(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized("Inquilino não identificado no contexto da requisição.");
        }

        var result = await _getApiKeysUseCase.ExecuteAsync(tenantId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Revoga o acesso de uma chave de API específica do Tenant autenticado.
    /// </summary>
    [Authorize]
    [HttpDelete("api-keys/{keyHash}")]
    public async Task<ActionResult> RevokeApiKey(string keyHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyHash))
        {
            return BadRequest("O hash da chave é obrigatório.");
        }

        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Unauthorized("Inquilino não identificado no contexto da requisição.");
        }

        var result = await _revokeApiKeyUseCase.ExecuteAsync(tenantId, keyHash, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Parâmetros de criação de Tenant.
/// </summary>
public record CreateTenantRequest(string Name, string MainDomain);

/// <summary>
/// Parâmetros de geração de chave de API.
/// </summary>
public record GenerateApiKeyRequest(string Description);
