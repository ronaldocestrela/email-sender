using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Identity.Application.Ports;
using Identity.Domain.Aggregates;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Provedor concreto para geração de Tokens JWT.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gera um Token JWT para o usuário autenticado contendo Claims essenciais de TenantId e Role.
    /// </summary>
    public string GenerateToken(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var secretKey = _configuration["JwtSettings:Secret"] 
            ?? "SuperSecretSecurityKeyThatNeedsToBeLongEnoughLengthOf32Bytes!!!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "EmailSender";
        var audience = _configuration["JwtSettings:Audience"] ?? "EmailSender";
        var expirationHours = double.TryParse(_configuration["JwtSettings:ExpirationInHours"], out var hours) ? hours : 2;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("TenantId", user.TenantId.ToString()),
            new("MfaEnabled", user.IsMfaEnabled.ToString().ToLowerInvariant())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
