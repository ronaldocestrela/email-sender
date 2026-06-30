using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using Gateway.Bootstrapper.Contexts;
using Gateway.Bootstrapper.Middlewares;
using TenantManagement.Domain.Ports;
using TenantManagement.Application.UseCases;
using TenantManagement.Infrastructure.Persistence;
using TenantManagement.Infrastructure.Persistence.Repositories;
using Identity.Application.Ports;
using Identity.Application.UseCases;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Security;
using EmailEngine.Infrastructure.Persistence;
using EmailEngine.Infrastructure.Persistence.Repositories;
using EmailEngine.Infrastructure.Consumers;
using EmailEngine.Infrastructure.EmailSenders;
using EmailEngine.Application.Ports;
using EmailEngine.Application.UseCases;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Registro do serviço CORS para permitir chamadas do Frontend Blazor
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 1. Configurações de Conexão com Banco de Dados SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada.");

// 2. Registro do Provedor de Tenant Real (AsyncLocal) para todos os módulos
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<TenantManagement.Application.Ports.ITenantProvider>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<Identity.Application.Ports.ITenantProvider>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<EmailEngine.Application.Ports.ITenantProvider>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<EmailEngine.Application.Ports.ITenantSetter>(sp => sp.GetRequiredService<TenantContext>());

// 3. Registro de Casos de Uso e Repositórios de Tenant Management
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ICreateTenantUseCase, CreateTenantUseCase>();
builder.Services.AddScoped<IGenerateApiKeyUseCase, GenerateApiKeyUseCase>();
builder.Services.AddScoped<IGetTenantsUseCase, GetTenantsUseCase>();
builder.Services.AddScoped<IGetApiKeysUseCase, GetApiKeysUseCase>();
builder.Services.AddScoped<IRevokeApiKeyUseCase, RevokeApiKeyUseCase>();

// 4. Registro de Adapters, Serviços e Casos de Uso do Módulo Identity
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IMfaUseCase, MfaUseCase>();

// 5. Registro de Casos de Uso, Adapters de Envio e Repositórios do Email Engine
builder.Services.AddScoped<ISendEmailUseCase, SendEmailUseCase>();
builder.Services.AddScoped<IGetEmailHistoryUseCase, GetEmailHistoryUseCase>();
builder.Services.AddScoped<IEmailHistoryRepository, EmailHistoryRepository>();
builder.Services.AddScoped<IEmailProviderSettingsRepository, EmailProviderSettingsRepository>();

// Registro dos Adapters Físicos de Envio de E-mail
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddHttpClient<SendGridEmailSender>();
builder.Services.AddScoped<IEmailSender, CompositeEmailSender>();

// 6. Configuração de Autenticação JWT Bearer padrão
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var secretKey = builder.Configuration["JwtSettings:Secret"] 
        ?? "SuperSecretSecurityKeyThatNeedsToBeLongEnoughLengthOf32Bytes!!!";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "EmailSender",
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "EmailSender",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// 7. Registro dos DbContexts com Tabelas de Histórico Separadas
builder.Services.AddDbContext<TenantManagementDbContext>(options =>
    options.UseSqlServer(connectionString, o => 
        o.MigrationsHistoryTable("__TenantManagementMigrationsHistory")));

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(connectionString, o => 
        o.MigrationsHistoryTable("__IdentityMigrationsHistory")));

builder.Services.AddDbContext<EmailEngineDbContext>(options =>
    options.UseSqlServer(connectionString, o => 
        o.MigrationsHistoryTable("__EmailEngineMigrationsHistory")));

// 8. Configuração do MassTransit com RabbitMQ (Versão 8.3.6)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendEmailConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Configuração de endpoint com política de retentativas
        cfg.ReceiveEndpoint("send-email-command", e =>
        {
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            e.ConfigureConsumer<SendEmailConsumer>(context);
        });
    });
});

// 9. Registro dos Controllers e OpenAPI com esquema de segurança customizado
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new System.Collections.Generic.Dictionary<string, IOpenApiSecurityScheme>();

        // 1. Configuração do esquema de autenticação JWT Bearer
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Insira o token JWT no formato: Bearer {token}"
        });

        // 2. Configuração do esquema de cabeçalho X-API-KEY
        document.Components.SecuritySchemes.Add("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-API-KEY",
            In = ParameterLocation.Header,
            Description = "Chave de acesso da API do Tenant (injetada nas requisições HTTP de disparo)"
        });

        // 3. Aplica segurança de forma a permitir testes manuais no Scalar Playground
        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new System.Collections.Generic.List<string>(),
            [new OpenApiSecuritySchemeReference("ApiKey", document)] = new System.Collections.Generic.List<string>()
        };

        document.Security ??= new System.Collections.Generic.List<OpenApiSecurityRequirement>();
        document.Security.Add(securityRequirement);

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// 10. Execução automática das Migrações Pendentes no Startup
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
    
    // Configura a rota do Scalar para documentação interativa moderna
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Email Sender Multi-Tenant API")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// 11. Registro do Middleware de Resolução de Tenant e Autenticação
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
