using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantManagement.Domain.Aggregates;
using TenantManagement.Domain.ValueObjects;
using TenantManagement.Infrastructure.Persistence;
using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Identity.Application.Ports;

namespace Gateway.Bootstrapper.Persistence;

/// <summary>
/// Responsável pelo semeamento inicial do banco de dados (Seeder) com dados de teste/homologação.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Executa o seed de dados se o banco de dados de Tenants estiver vazio.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILogger<Program>>();
        var tenantContext = sp.GetRequiredService<TenantManagementDbContext>();
        var identityContext = sp.GetRequiredService<IdentityDbContext>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher>();

        try
        {
            // 1. Verifica se já existem Tenants cadastrados no sistema
            if (await tenantContext.Tenants.AnyAsync())
            {
                logger.LogInformation("O banco de dados já possui inquilinos cadastrados. Pulando semeamento.");
                return;
            }

            logger.LogWarning("Nenhum inquilino encontrado. Iniciando o processo de Seed automático...");

            // 2. Criar o Tenant Principal (Administrativo)
            var tenantResult = Tenant.Create("Admin Tenant");
            if (tenantResult.IsFailure)
            {
                logger.LogError("Falha ao criar Tenant de semente: {Error}", tenantResult.Error.Message);
                return;
            }

            var tenant = tenantResult.Value;

            // 3. Adicionar Domínio Principal Associado
            var domainResult = DomainName.Create("admintent.com");
            if (domainResult.IsFailure)
            {
                logger.LogError("Falha ao criar Domínio de semente: {Error}", domainResult.Error.Message);
                return;
            }

            tenant.AddDomain(domainResult.Value);

            // 4. Gerar Chave de API de Teste
            var apiKeyResult = tenant.GenerateApiKey("Chave de Teste Inicial");
            if (apiKeyResult.IsFailure)
            {
                logger.LogError("Falha ao criar ApiKey de semente: {Error}", apiKeyResult.Error.Message);
                return;
            }

            var apiKey = apiKeyResult.Value;

            // Persiste o Tenant e a ApiKey
            await tenantContext.Tenants.AddAsync(tenant);
            await tenantContext.SaveChangesAsync();

            // Exibe a chave gerada de forma chamativa no console
            logger.LogWarning("======================================================================");
            logger.LogWarning(" CHAVE DE API PARA DISPAROS (PLAIN TEXT):");
            logger.LogWarning(" {Key}", apiKey.PlainTextKey);
            logger.LogWarning("======================================================================");

            // 5. Criar o Usuário Administrador vinculado ao Tenant
            var passwordHash = passwordHasher.HashPassword("Admin@123");
            var userResult = User.Create(
                "admin@admintent.com",
                passwordHash,
                tenant.Id,
                "Admin"
            );

            if (userResult.IsFailure)
            {
                logger.LogError("Falha ao criar Usuário Administrador de semente: {Error}", userResult.Error.Message);
                return;
            }

            await identityContext.Users.AddAsync(userResult.Value);
            await identityContext.SaveChangesAsync();

            logger.LogWarning("Seed executado com sucesso!");
            logger.LogWarning("Usuário: admin@admintent.com | Senha: Admin@123");
            logger.LogWarning("======================================================================");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado durante a execução do semeamento do banco.");
        }
    }
}
