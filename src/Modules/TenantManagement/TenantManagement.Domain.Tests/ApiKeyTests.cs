using Xunit;
using TenantManagement.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace TenantManagement.Domain.Tests;

/// <summary>
/// Testes unitários para o gerenciamento de ApiKey.
/// </summary>
public class ApiKeyTests
{
    [Fact]
    public void GivenValidRequest_WhenApiKeyCreated_ShouldGenerateSecureKey()
    {
        // Arrange
        var description = "Test Key";

        // Act
        var result = ApiKey.Create(description);

        // Assert
        Assert.True(result.IsSuccess);
        var apiKey = result.Value;
        Assert.NotNull(apiKey);
        Assert.Equal(description, apiKey.Description);
        Assert.StartsWith("es_live_", apiKey.PlainTextKey);
        Assert.True(apiKey.PlainTextKey.Length > 20);
        Assert.Equal(64, apiKey.KeyHash.Length); // Hash SHA256 em hexadecimal tem 64 caracteres
        Assert.False(apiKey.IsRevoked);
    }

    [Fact]
    public void GivenActiveApiKey_WhenRevoked_ShouldSetIsRevokedToTrue()
    {
        // Arrange
        var apiKey = ApiKey.Create("Prod integration").Value;

        // Act
        apiKey.Revoke();

        // Assert
        Assert.True(apiKey.IsRevoked);
    }
}
