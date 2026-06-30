using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Application.UseCases;

namespace Gateway.Bootstrapper.Controllers;

/// <summary>
/// Controlador responsável pelas operações de autenticação e gerenciamento de MFA.
/// </summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly ILoginUseCase _loginUseCase;
    private readonly IMfaUseCase _mfaUseCase;

    public AuthController(ILoginUseCase loginUseCase, IMfaUseCase mfaUseCase)
    {
        _loginUseCase = loginUseCase;
        _mfaUseCase = mfaUseCase;
    }

    /// <summary>
    /// Efetua o login do usuário, retornando o token JWT ou indicando que MFA é requerido.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Corpo da requisição inválido.");
        }

        var result = await _loginUseCase.ExecuteAsync(request.Email, request.Password, request.MfaCode, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Inicializa a configuração do MFA (TOTP), gerando o segredo e URI de QR Code.
    /// </summary>
    [Authorize]
    [HttpPost("mfa/setup")]
    public async Task<ActionResult> SetupMfa(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Usuário não identificado.");
        }

        var result = await _mfaUseCase.SetupMfaAsync(userId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Confirma e ativa definitivamente o MFA informando o código verificado e o segredo temporário.
    /// </summary>
    [Authorize]
    [HttpPost("mfa/confirm")]
    public async Task<ActionResult> ConfirmMfa([FromBody] ConfirmMfaRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Corpo da requisição inválido.");
        }

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Usuário não identificado.");
        }

        var result = await _mfaUseCase.ConfirmMfaAsync(userId, request.Code, request.Secret, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Desabilita o MFA do usuário autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("mfa/disable")]
    public async Task<ActionResult> DisableMfa(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Usuário não identificado.");
        }

        var result = await _mfaUseCase.DisableMfaAsync(userId, cancellationToken);
        return HandleResult(result);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) 
            ?? User.FindFirst("sub");

        if (claim != null && Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}

/// <summary>
/// Parâmetros de login.
/// </summary>
public record LoginRequest(string Email, string Password, string? MfaCode);

/// <summary>
/// Parâmetros de confirmação de MFA.
/// </summary>
public record ConfirmMfaRequest(string Code, string Secret);
