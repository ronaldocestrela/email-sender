using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Gateway.Bootstrapper.Controllers;

/// <summary>
/// Controlador base da API que fornece utilitários comuns para traduzir o padrão Result
/// dos diferentes módulos para respostas HTTP estruturadas padrão.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // --- Sobrecargas para o módulo Identity ---

    protected ActionResult HandleResult(Identity.Domain.Common.Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return MapIdentityFailure(result.Error);
    }

    protected ActionResult HandleResult<T>(Identity.Domain.Common.Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return MapIdentityFailure(result.Error);
    }

    private ActionResult MapIdentityFailure(Identity.Domain.Common.Error error)
    {
        return error.Code switch
        {
            var code when code.Contains("NotFound", StringComparison.OrdinalIgnoreCase) => NotFound(new { error.Code, error.Message }),
            var code when code.Contains("Credentials", StringComparison.OrdinalIgnoreCase) || code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) => StatusCode(StatusCodes.Status401Unauthorized, new { error.Code, error.Message }),
            _ => BadRequest(new { error.Code, error.Message })
        };
    }

    // --- Sobrecargas para o módulo TenantManagement ---

    protected ActionResult HandleResult(TenantManagement.Domain.Common.Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return MapTenantFailure(result.Error);
    }

    protected ActionResult HandleResult<T>(TenantManagement.Domain.Common.Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return MapTenantFailure(result.Error);
    }

    private ActionResult MapTenantFailure(TenantManagement.Domain.Common.Error error)
    {
        return error.Code switch
        {
            var code when code.Contains("NotFound", StringComparison.OrdinalIgnoreCase) => NotFound(new { error.Code, error.Message }),
            var code when code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) => StatusCode(StatusCodes.Status401Unauthorized, new { error.Code, error.Message }),
            _ => BadRequest(new { error.Code, error.Message })
        };
    }

    // --- Sobrecargas para o módulo EmailEngine ---

    protected ActionResult HandleResult(EmailEngine.Domain.Common.Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return MapEmailFailure(result.Error);
    }

    protected ActionResult HandleResult<T>(EmailEngine.Domain.Common.Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return MapEmailFailure(result.Error);
    }

    private ActionResult MapEmailFailure(EmailEngine.Domain.Common.Error error)
    {
        return error.Code switch
        {
            var code when code.Contains("NotFound", StringComparison.OrdinalIgnoreCase) => NotFound(new { error.Code, error.Message }),
            var code when code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) => StatusCode(StatusCodes.Status401Unauthorized, new { error.Code, error.Message }),
            _ => BadRequest(new { error.Code, error.Message })
        };
    }
}
