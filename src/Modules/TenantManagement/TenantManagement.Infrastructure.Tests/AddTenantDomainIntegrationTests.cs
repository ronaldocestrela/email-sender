using Microsoft.EntityFrameworkCore;
using TenantManagement.Application.UseCases;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Infrastructure.Persistence.Repositories;
using TenantManagement.Infrastructure.Tests.Support;

namespace TenantManagement.Infrastructure.Tests;

/// <summary>
/// Testes de integração para persistência de domínios via AddTenantDomainUseCase.
/// </summary>
public class AddTenantDomainIntegrationTests
{
    /// <summary>
    /// Garante que um domínio adicionado a um tenant existente seja persistido na tabela TenantDomains.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenAddingValidDomain_ShouldPersistDomainInDatabase()
    {
        // Given
        var tenantResult = Tenant.Create("Empresa Teste");
        Assert.True(tenantResult.IsSuccess);
        var tenant = tenantResult.Value;

        var tenantProvider = new TestTenantProvider(tenant.Id);
        await using var connection = TenantDbContextFactory.CreateSharedConnection();

        await using (var seedContext = TenantDbContextFactory.CreateContext(connection, tenantProvider))
        {
            var seedRepository = new TenantRepository(seedContext);
            await seedRepository.AddAsync(tenant);
        }

        // When
        await using (var writeContext = TenantDbContextFactory.CreateContext(connection, tenantProvider, ensureCreated: false))
        {
            var repository = new TenantRepository(writeContext);
            var useCase = new AddTenantDomainUseCase(repository);
            var result = await useCase.ExecuteAsync(tenant.Id, "disparos.empresa.com");

            Assert.True(result.IsSuccess);
            Assert.Equal("disparos.empresa.com", result.Value!.Value);
        }

        // Then — recarrega em contexto limpo
        await using (var readContext = TenantDbContextFactory.CreateContext(connection, tenantProvider, ensureCreated: false))
        {
            readContext.ChangeTracker.Clear();

            var reloaded = await readContext.Tenants
                .Include(t => t.LinkedDomains)
                .FirstOrDefaultAsync(t => t.Id == tenant.Id);

            Assert.NotNull(reloaded);
            Assert.Single(reloaded.LinkedDomains);
            Assert.Equal("disparos.empresa.com", reloaded.LinkedDomains.First().Value);
        }
    }

    /// <summary>
    /// Garante que domínios inválidos não sejam persistidos no banco.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenDomainIsInvalid_ShouldNotPersistDomain()
    {
        // Given
        var tenantResult = Tenant.Create("Empresa Teste");
        Assert.True(tenantResult.IsSuccess);
        var tenant = tenantResult.Value;

        var tenantProvider = new TestTenantProvider(tenant.Id);
        await using var connection = TenantDbContextFactory.CreateSharedConnection();
        await using var context = TenantDbContextFactory.CreateContext(connection, tenantProvider);

        var repository = new TenantRepository(context);
        await repository.AddAsync(tenant);

        var useCase = new AddTenantDomainUseCase(repository);

        // When
        var result = await useCase.ExecuteAsync(tenant.Id, "dominio-invalido");

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal("DomainName.Invalid", result.Error.Code);

        context.ChangeTracker.Clear();
        var reloaded = await context.Tenants
            .Include(t => t.LinkedDomains)
            .FirstAsync(t => t.Id == tenant.Id);

        Assert.Empty(reloaded.LinkedDomains);
    }

    /// <summary>
    /// Garante que domínios duplicados não sejam persistidos novamente.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenDomainIsDuplicate_ShouldNotPersistSecondEntry()
    {
        // Given
        var tenantResult = Tenant.Create("Empresa Teste");
        Assert.True(tenantResult.IsSuccess);
        var tenant = tenantResult.Value;
        tenant.AddDomain("mail.empresa.com", isVerified: true);

        var tenantProvider = new TestTenantProvider(tenant.Id);
        await using var connection = TenantDbContextFactory.CreateSharedConnection();
        await using var context = TenantDbContextFactory.CreateContext(connection, tenantProvider);

        var repository = new TenantRepository(context);
        await repository.AddAsync(tenant);

        var useCase = new AddTenantDomainUseCase(repository);

        // When
        var result = await useCase.ExecuteAsync(tenant.Id, "mail.empresa.com");

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.DuplicateDomain", result.Error.Code);

        context.ChangeTracker.Clear();
        var reloaded = await context.Tenants
            .Include(t => t.LinkedDomains)
            .FirstAsync(t => t.Id == tenant.Id);

        Assert.Single(reloaded.LinkedDomains);
    }

    /// <summary>
    /// Garante que UpdateAsync persista domínios quando o tenant já está rastreado pelo contexto.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenTenantIsTracked_ShouldPersistAddedDomain()
    {
        // Given
        var tenantResult = Tenant.Create("Empresa Teste");
        Assert.True(tenantResult.IsSuccess);
        var tenant = tenantResult.Value;

        var tenantProvider = new TestTenantProvider(tenant.Id);
        await using var connection = TenantDbContextFactory.CreateSharedConnection();
        await using var context = TenantDbContextFactory.CreateContext(connection, tenantProvider);

        var repository = new TenantRepository(context);
        await repository.AddAsync(tenant);

        var trackedTenant = await repository.GetByIdAsync(tenant.Id);
        Assert.NotNull(trackedTenant);
        trackedTenant.AddDomain("novo.empresa.com");

        // When
        await repository.UpdateAsync(trackedTenant);

        // Then
        context.ChangeTracker.Clear();
        var reloaded = await context.Tenants
            .Include(t => t.LinkedDomains)
            .FirstAsync(t => t.Id == tenant.Id);

        Assert.Single(reloaded.LinkedDomains);
        Assert.Equal("novo.empresa.com", reloaded.LinkedDomains.First().Value);
    }
}
