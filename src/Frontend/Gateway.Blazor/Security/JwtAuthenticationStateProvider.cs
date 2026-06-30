using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Gateway.Blazor.Services;

namespace Gateway.Blazor.Security;

/// <summary>
/// Provedor customizado de Estado de Autenticação baseado em Tokens JWT armazenados no LocalStorage.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly LocalStorageService _localStorage;
    private const string AuthTokenKey = "authToken";
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public JwtAuthenticationStateProvider(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Recupera o estado atual de autenticação lendo o token do LocalStorage.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(_anonymous);
            }

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.NameIdentifier, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    /// <summary>
    /// Marca o usuário como autenticado salvando o token e notificando o Blazor.
    /// </summary>
    public async Task MarkUserAsAuthenticated(string token)
    {
        await _localStorage.SetItemAsync(AuthTokenKey, token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.NameIdentifier, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        var authState = Task.FromResult(new AuthenticationState(principal));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Marca o usuário como deslogado limpando o token e notificando o Blazor.
    /// </summary>
    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(AuthTokenKey);
        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var segments = jwt.Split('.');
        if (segments.Length < 2)
        {
            return claims;
        }

        var payload = segments[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                var valueStr = kvp.Value?.ToString() ?? string.Empty;
                var key = kvp.Key;

                // Mapeamentos comuns do JWT para Claims do .NET
                if (key == "role" || key == ClaimTypes.Role)
                {
                    if (valueStr.StartsWith("["))
                    {
                        var roles = JsonSerializer.Deserialize<string[]>(valueStr);
                        if (roles != null)
                        {
                            foreach (var role in roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, valueStr));
                    }
                }
                else if (key == "sub" || key == "nameid" || key == ClaimTypes.NameIdentifier)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, valueStr));
                }
                else if (key == "email" || key == ClaimTypes.Email)
                {
                    claims.Add(new Claim(ClaimTypes.Email, valueStr));
                }
                else
                {
                    claims.Add(new Claim(key, valueStr));
                }
            }
        }

        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        // Normaliza base64url para base64 padrão
        base64 = base64.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
