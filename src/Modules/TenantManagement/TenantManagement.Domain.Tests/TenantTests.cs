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
        
        // Crio um mock temporário de DomainName na fase Red. 
        // Como o Create de DomainName retorna falha por padrão na fase RED,
        // nos testes unitários vamos instanciar ou assumir que o DomainName passe (ou mockar).
        // Mas como DomainName é um record, podemos forçar a criação para o teste ou usar a factory.
        // Esperamos que na fase GREEN o DomainName.Create retorne sucesso.
        var domainResult = DomainName.Create("acme.com");
        Assert.True(domainResult.IsSuccess, "DomainName creation should pass in Phase Green");
        var domain = domainResult.Value;

        // Act
        var result = tenant.AddDomain(domain);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(domain, tenant.LinkedDomains);
    }

    [Fact]
    public void GivenTenantWithDomain_WhenAddingDuplicateDomain_ShouldReturnFailure()
    {
        // Arrange
        var tenant = Tenant.Create("Acme").Value;
        var domain = DomainName.Create("acme.com").Value;
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
}
