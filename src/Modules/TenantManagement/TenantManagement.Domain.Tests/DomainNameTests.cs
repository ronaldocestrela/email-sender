using Xunit;
using TenantManagement.Domain.ValueObjects;

namespace TenantManagement.Domain.Tests;

/// <summary>
/// Testes unitários para o Value Object DomainName.
/// </summary>
public class DomainNameTests
{
    [Theory]
    [InlineData("example.com")]
    [InlineData("sub.example.com")]
    [InlineData("my-domain.co.uk")]
    [InlineData("domain.com.br")]
    public void GivenValidDomain_WhenCreated_ShouldReturnSuccess(string validDomain)
    {
        // Act
        var result = DomainName.Create(validDomain);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(validDomain, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/api")]
    [InlineData("user@example.com")]
    [InlineData("invalid_domain")]
    [InlineData(".com")]
    public void GivenInvalidDomain_WhenCreated_ShouldReturnFailure(string invalidDomain)
    {
        // Act
        var result = DomainName.Create(invalidDomain);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("DomainName.Invalid", result.Error.Code);
    }
}
