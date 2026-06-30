# Composição e Gateway Bootstrapper (Gateway.Bootstrapper)

Este projeto serve como a raiz de composição (**Composition Root**) do monólito modular, hospedando o pipeline HTTP de execução, a inicialização e fiação de injeção de dependências (DI) de todos os módulos, a autenticação global e a geração da documentação da API. Ele reside sob o diretório [Gateway.Bootstrapper](file:///home/rony/LPR/email-sender/src/Gateway.Bootstrapper).

---

## 1. Responsabilidade Arquitetural

Como monólito modular, os módulos não possuem referências diretas de banco de dados ou acoplamento forte entre si. O projeto `Gateway.Bootstrapper` orquestra:
1. A injeção das implementações de infraestrutura de cada módulo para atender às suas respectivas portas de aplicação.
2. A configuração física dos DbContexts (cada módulo possui suas tabelas separadas, mas compartilhando a mesma string de conexão e base SQL Server).
3. A resolução do contexto de multi-inquilinato (Multi-Tenancy).

---

## 2. Motor Multi-Tenancy

O isolamento é baseado no padrão **Single Database com Isolamento Lógico (coluna TenantId)**. A arquitetura é suportada por três pilares:

### A. Contexto Seguro com AsyncLocal (`TenantContext`)
* Localizado em: [TenantContext.cs](file:///home/rony/LPR/email-sender/src/Gateway.Bootstrapper/Contexts/TenantContext.cs)
* Implementa as interfaces `ITenantProvider` (dos três módulos) e `ITenantSetter` (usada no consumidor de mensagens de background).
* Armazena o ID do inquilino ativo em um campo `AsyncLocal<Guid>`. Isso garante isolamento estrito de threads, prevenindo vazamento de dados de inquilinos em chamadas HTTP assíncronas concorrentes.

### B. Middleware de Resolução (`TenantResolutionMiddleware`)
* Localizado em: [TenantResolutionMiddleware.cs](file:///home/rony/LPR/email-sender/src/Gateway.Bootstrapper/Middlewares/TenantResolutionMiddleware.cs)
* Intercepta a requisição HTTP e resolve o Tenant por duas vias:
  1. **Header X-API-KEY:** Se presente, calcula o hash SHA-256 da chave e consulta no banco de dados (`GetByApiKeyHashAsync`). Se for válida, popula o `TenantContext`.
  2. **Token JWT:** Se autenticado, lê a claim `"TenantId"` injetada no token no momento do login e popula o `TenantContext`.
* Se nenhuma credencial for informada, o contexto permanece vazio (caso de endpoints públicos como login ou cadastro administrativo de Tenant).

### C. Filtros Globais e Salvamento Automático (EF Core)
* Cada `DbContext` (ex: [EmailEngineDbContext](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Infrastructure/Persistence/EmailEngineDbContext.cs)) possui em seu `OnModelCreating` uma expressão lambda gerada dinamicamente:
  `x => x.TenantId == CurrentTenantId`
* O EF Core anexa esse filtro de forma implícita em **todas** as queries SQL executadas contra tabelas que mapeiam entidades `IMustHaveTenant`.
* Adicionalmente, sobrescrevemos o método `SaveChangesAsync` para capturar qualquer entidade sendo inserida e injetar automaticamente o `CurrentTenantId` resolvido.

---

## 3. Pipeline HTTP e Middleware (Program.cs)

A ordem dos middlewares no [Program.cs](file:///home/rony/LPR/email-sender/src/Gateway.Bootstrapper/Program.cs) é estrita:

```text
       Requisição HTTP
              │
              ▼
    [ app.UseAuthentication() ]        // Autentica o JWT se houver
              │
              ▼
    [ app.UseMiddleware<...>() ]       // TenantResolutionMiddleware lê claims autenticadas
              │
              ▼
    [ app.UseAuthorization() ]         // Valida políticas e cargos
              │
              ▼
    [ app.MapControllers() ]           // Executa o Endpoint
```

* **Autenticação JWT:** Configurada no container DI utilizando autenticação padrão baseada em token Bearer simétrico (`Microsoft.AspNetCore.Authentication.JwtBearer`).
* **OpenAPI + Scalar UI:** O Scalar é exposto em `/scalar/v1` em modo de Desenvolvimento, fornecendo o playground interativo já mapeado com os esquemas de autenticação Bearer e ApiKey.
* **Migrações Automáticas:** No startup, o monólito recupera os três DbContexts sequencialmente e executa o método `Database.MigrateAsync()`, garantindo que tabelas compartilhadas ou separadas existam em conformidade com o modelo antes de aceitar tráfego HTTP.

---

## 4. Diretrizes de Manutenção

1. **Adicionar Novos Middlewares:** Sempre posicione novos middlewares de auditoria ou logging *depois* do `TenantResolutionMiddleware` caso precise associar o log ao `TenantId` correspondente da requisição.
2. **Atualizar Configurações do JWT:** Alterações de tempo de expiração ou chaves secretas de assinatura devem ser parametrizadas no `appsettings.json` na seção `JwtSettings` para manter a consistência com o `TokenService`.
