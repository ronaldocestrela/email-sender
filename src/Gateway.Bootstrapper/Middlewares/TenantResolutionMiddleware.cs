using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Gateway.Bootstrapper.Contexts;
using TenantManagement.Domain.Ports;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Bootstrapper.Middlewares;

/// <summary>
/// Middleware HTTP para resolução e injeção do TenantId com base no cabeçalho X-API-KEY ou Claims JWT.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invoca o middleware no fluxo da requisição HTTP para resolver o tenant.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ITenantRepository tenantRepository)
    {
        // 1. Tentar resolver pelo cabeçalho X-API-KEY (Integrações Externas / API de envio)
        if (context.Request.Headers.TryGetValue("X-API-KEY", out var apiKeyValues))
        {
            var apiKey = apiKeyValues.ToString();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var hashedKey = HashApiKey(apiKey);
                
                // Busca administrativa ignorando filtros globais
                var tenant = await tenantRepository.GetByApiKeyHashAsync(hashedKey, context.RequestAborted);
                if (tenant != null && tenant.IsActive)
                {
                    TenantContext.SetCurrentTenant(tenant.Id);
                    context.Items["TenantId"] = tenant.Id;
                }
            }
        }
        // 2. Tentar resolver pela Claim de TenantId do Token JWT (Acesso ao Dashboard)
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<TenantResolutionMiddleware>>();
            logger.LogWarning("DEBUG: Autenticado. Total Claims: {Count}", context.User.Claims.Count());
            foreach (var claim in context.User.Claims)
            {
                logger.LogWarning("DEBUG: Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }

            var tenantIdClaim = context.User.Claims.FirstOrDefault(c => c.Type.Equals("TenantId", StringComparison.OrdinalIgnoreCase));
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                logger.LogWarning("DEBUG: TenantId resolvido com sucesso: {TenantId}", tenantId);
                TenantContext.SetCurrentTenant(tenantId);
                context.Items["TenantId"] = tenantId;
            }
            else
            {
                logger.LogWarning("DEBUG: Claim TenantId NÃO encontrada ou inválida. tenantIdClaim é nulo: {IsNull}", tenantIdClaim == null);
            }
        }
        else
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<TenantResolutionMiddleware>>();
            logger.LogWarning("DEBUG: Usuário NÃO está autenticado no middleware.");
        }

        await _next(context);
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
