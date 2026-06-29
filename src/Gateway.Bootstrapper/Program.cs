using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Gateway.Bootstrapper.Providers;
using TenantManagement.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using EmailEngine.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurações de Conexão com Banco de Dados SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada.");

// 2. Registro do Provedor de Tenant temporário para todos os módulos
builder.Services.AddScoped<MockTenantProvider>();
builder.Services.AddScoped<TenantManagement.Application.Ports.ITenantProvider>(sp => sp.GetRequiredService<MockTenantProvider>());
builder.Services.AddScoped<Identity.Application.Ports.ITenantProvider>(sp => sp.GetRequiredService<MockTenantProvider>());
builder.Services.AddScoped<EmailEngine.Application.Ports.ITenantProvider>(sp => sp.GetRequiredService<MockTenantProvider>());

// 3. Registro dos DbContexts com Tabelas de Histórico Separadas
builder.Services.AddDbContext<TenantManagementDbContext>(options =>
    options.UseSqlServer(connectionString, o => 
        o.MigrationsHistoryTable("__TenantManagementMigrationsHistory")));

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(connectionString, o => 
        o.MigrationsHistoryTable("__IdentityMigrationsHistory")));

builder.Services.AddDbContext<EmailEngineDbContext>(options =>
    options.UseSqlServer(connectionString, o => 
        o.MigrationsHistoryTable("__EmailEngineMigrationsHistory")));

// 4. Registro dos Controllers e OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Execução automática das Migrações Pendentes no Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Iniciando migração do banco de dados...");

        logger.LogInformation("Aplicando migrações do TenantManagement...");
        var tenantContext = services.GetRequiredService<TenantManagementDbContext>();
        await tenantContext.Database.MigrateAsync();

        logger.LogInformation("Aplicando migrações do Identity...");
        var identityContext = services.GetRequiredService<IdentityDbContext>();
        await identityContext.Database.MigrateAsync();

        logger.LogInformation("Aplicando migrações do EmailEngine...");
        var emailContext = services.GetRequiredService<EmailEngineDbContext>();
        await emailContext.Database.MigrateAsync();

        logger.LogInformation("Todas as migrações aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocorreu um erro catastrófico ao migrar os bancos de dados.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
