using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Gateway.Blazor;
using Gateway.Blazor.Services;
using Gateway.Blazor.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Registro de serviços auxiliares e segurança
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddTransient<AuthHeaderHandler>();

// 2. Registro do provedor de estado de autenticação JWT
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// 3. Configuração do HttpClient customizado apontando para o Gateway Backend
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();
    // Em Blazor WebAssembly, usamos HttpClientHandler que delega para o fetch da Web do Navegador
    handler.InnerHandler = new HttpClientHandler(); 
    
    return new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:5090/")
    };
});

await builder.Build().RunAsync();
