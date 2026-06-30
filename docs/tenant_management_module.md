# Módulo Tenant Management (TenantManagement)

Este módulo gerencia o isolamento lógico do ecossistema SaaS, controlando o cadastro de inquilinos, associação de domínios corporativos e geração/revogação de credenciais externas (`ApiKeys`). Ele reside sob o diretório [Modules/TenantManagement](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement).

---

## 1. Arquitetura do Módulo

O módulo segue a divisão hexagonal com isolamento absoluto de dados:

```text
       ┌────────────────────────────────────────────────────────┐
       │             TenantManagement.Application               │
       │                      (Use Cases)                       │
       │        CreateTenantUseCase, GenerateApiKeyUseCase       │
       └──────────┬──────────────────────────────────┬──────────┘
                  │ (Chama)                          │ (Implementa)
                  ▼                                  ▼
    ┌───────────────────────────┐         ┌───────────────────────────┐
    │  TenantManagement.Domain  │         │TenantManagement.Appl.Ports│
    │  (Entities & Aggregates)  │         │   (Outbound Interfaces)   │
    │    Tenant, DomainName     │         │     ITenantRepository     │
    └───────────────────────────┘         └─────────────┬─────────────┘
                                                        │ (Implementa)
                                                        ▼
                                          ┌───────────────────────────┐
                                          │TenantManagement.Infrastr. │
                                          │   (EF Core Repositories,  │
                                          │    Owned Entity Mapping)  │
                                          └───────────────────────────┘
```

---

## 2. Camada de Domínio (`TenantManagement.Domain`)

Centraliza os conceitos de inquilinato utilizando agregados com invariantes rígidas.

* **Tenant (Aggregate Root):** [Tenant.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Domain/Aggregates/Tenant.cs)
  * Representa o cliente corporativo (Tenant). Não implementa `IMustHaveTenant`, pois ele próprio é a entidade principal de isolamento.
  * Mantém coleções privadas encapsuladas de domínios e chaves:
    * `LinkedDomains`: Lista de domínios autenticados associados ao Tenant.
    * `ApiKeys`: Lista de chaves de acesso.
* **DomainName (Value Object):** [DomainName.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Domain/ValueObjects/DomainName.cs)
  * Valida a sintaxe do domínio (regex RFC) e garante letras minúsculas (normalização).
* **ApiKey (Entity):** [ApiKey.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Domain/Entities/ApiKey.cs)
  * Credencial de autenticação.
  * **PlainTextKey (Chave em texto plano):** Gerada dinamicamente com caracteres seguros e exposta **apenas uma única vez** no momento de criação no DTO de resposta.
  * **KeyHash (Hash seguro):** Armazenada no banco como o hash SHA-256 do texto plano.
  * Fornece controle de expiração (`ExpiresAt`) e revogação manual (`IsActive`).

---

## 3. Camada de Aplicação (`TenantManagement.Application`)

* **Casos de Uso (Use Cases):**
  * `CreateTenantUseCase`: [CreateTenantUseCase.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Application/UseCases/CreateTenantUseCase.cs)
    * Cria a instância do Tenant.
    * Valida e associa o domínio principal via Value Object.
    * Persiste o Tenant recém-criado no repositório.
  * `GenerateApiKeyUseCase`: [GenerateApiKeyUseCase.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Application/UseCases/GenerateApiKeyUseCase.cs)
    * Carrega o Tenant correspondente pelo ID.
    * Invoca a geração de ApiKey de domínio, anexando-a à coleção do aggregate.
    * Salva as alterações e retorna a resposta com a chave em texto plano.

* **Portas de Saída (Ports):**
  * [ITenantRepository.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Domain/Ports/ITenantRepository.cs): Interface de acesso a dados.

---

## 4. Camada de Infraestrutura (`TenantManagement.Infrastructure`)

* **Persistence (EF Core):** [TenantManagementDbContext.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Infrastructure/Persistence/TenantManagementDbContext.cs)
  * Mapeia `DomainName` como uma coleção própria pertencente à tabela `TenantDomains`.
  * Mapeia `ApiKey` como uma tabela associada `TenantApiKeys`.
* **Repository Implementation:** [TenantRepository.cs](file:///home/rony/LPR/email-sender/src/Modules/TenantManagement/TenantManagement.Infrastructure/Persistence/Repositories/TenantRepository.cs)
  * **Bypass de Filtro Global no Middleware:** O middleware HTTP de autenticação executa `GetByApiKeyHashAsync` para resolver o `TenantId` da requisição. Como o context do Tenant ainda não está populado, esta consulta obrigatoriamente aplica `.IgnoreQueryFilters()` para poder buscar o hash nas chaves de API globais.

---

## 5. Manutenção e Diretrizes de Modificação

1. **Alterar Estrutura de Chave de API:** Se o tamanho ou formato do gerador criptográfico de ApiKey mudar, isso deve ser alterado no factory method `ApiKey.Create(...)` de domínio.
2. **Consultas a Nível Administrativo:** Sempre que for criada uma query administrativa global (ex: Admin geral do SaaS listando inquilinos), lembre-se de usar `.IgnoreQueryFilters()` no DbSet do EF Core para desviar do comportamento padrão que restringe registros ao inquilino da requisição atual.
