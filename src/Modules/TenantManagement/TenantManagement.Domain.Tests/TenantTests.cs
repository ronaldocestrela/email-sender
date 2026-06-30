using System;
using Xunit;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Domain.ValueObjects;
using System.Linq;

namespace TenantManagement.Domain.Tests;

/// <summary>
/// Testes unitários para a raiz do Agregado Tenant.
/// </summary>
public class TenantTests
{
    [Fact]
    public void GivenValidName_WhenTenantCreated_ShouldSucceedAndBeActive()
    {
        // Arrange
        var name = "Acme Corp";

        // Act
        var result = Tenant.Create(name);

        // Assert
        Assert.True(result.IsSuccess);
        var tenant = result.Value;
        Assert.NotNull(tenant);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal(name, tenant.Name);
        Assert.True(tenant.IsActive);
        Assert.True(tenant.CreatedAt <= DateTime.UtcNow);
        Assert.Empty(tenant.LinkedDomains);
        Assert.Empty(tenant.ApiKeys);
    }

    [Fact]
    public void GivenTenant_WhenAddingNewDomain_ShouldAppendToLinkedDomains()
    {
        // Arrange
        var tenant = Tenant.Create("Acme").Value;
        var domain = "acme.com";

        // Act
        var result = tenant.AddDomain(domain);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(tenant.LinkedDomains, d => d.Value == domain);
        var addedDomain = tenant.LinkedDomains.First(d => d.Value == domain);
        Assert.False(addedDomain.IsVerified);
        Assert.NotNull(addedDomain.VerificationToken);
    }

    [Fact]
    public void GivenTenantWithDomain_WhenAddingDuplicateDomain_ShouldReturnFailure()
    {
        // Arrange
        var tenant = Tenant.Create("Acme").Value;
        var domain = "acme.com";
        tenant.AddDomain(domain);

        // Act
        var result = tenant.AddDomain(domain);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.DuplicateDomain", result.Error.Code);
    }

    [Fact]
    public void GivenTenant_WhenGeneratingApiKey_ShouldAddToKeysCollection()
    {
        // Arrange
        var tenant = Tenant.Create("Acme").Value;
        var description = "Web App Integration";

        // Act
        var result = tenant.GenerateApiKey(description);

        // Assert
        Assert.True(result.IsSuccess);
        var apiKey = result.Value;
        Assert.NotNull(apiKey);
        Assert.Contains(apiKey, tenant.ApiKeys);
        Assert.Equal(description, apiKey.Description);
    }

    [Fact]
    public void GivenTenantWithApiKey_WhenRevokingApiKey_ShouldUpdateKeyStatus()
    {
        // Arrange
        var tenant = Tenant.Create("Acme").Value;
        var apiKey = tenant.GenerateApiKey("Temp Integration").Value;
        var hash = apiKey.KeyHash;

        // Act
        var result = tenant.RevokeApiKey(hash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(apiKey.IsRevoked);
    }

    [Fact]
    public void GivenTenantWithUnverifiedDomain_WhenVerifyingDomain_ShouldMarkAsVerified()
    {
        // Arrange
        var tenant = Tenant.Create("Acme").Value;
        var domain = "acme.com";
        tenant.AddDomain(domain);

        // Act
        var result = tenant.VerifyDomain(domain);

        // Assert
        Assert.True(result.IsSuccess);
        var tenantDomain = tenant.LinkedDomains.First(d => d.Value == domain);
        Assert.True(tenantDomain.IsVerified);
        Assert.NotNull(tenantDomain.VerifiedAt);
    }
}
