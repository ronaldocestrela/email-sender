# AGENTS.md - Diretrizes de Desenvolvimento e Instruções para Agente de IA

Este documento serve como a **Especificação de Engenharia e Guia de Contexto Absoluto** para o Agente de IA. Ele contém todas as regras, padrões arquiteturais, restrições tecnológicas e práticas de design necessárias para construir o sistema de disparo de e-mails multi-tenant de ponta a ponta.

---

## 1. Visão Geral da Solução e Stack Tecnológica

O sistema será construído como um **Monólito Modular** altamente coeso e fracamente acoplado, preparado para evolução futura e venda como produto SaaS (White-Label).

### Stack Tecnológica Core
* **Backend:** .NET 10 (ASP.NET Core Web API) com integração obrigatória ao **OpenAPI** e **Scalar** para documentação interativa da API.
* **Frontend:** Blazor (Interactive Auto ou WebAssembly Mode para máxima performance e separação de UI).
* **Persistência:** Entity Framework Core 10 com **SQL Server** e isolamento lógico via coluna `TenantId`.
* **Autenticação/Autorização:** Microsoft Identity Framework adaptado para arquitetura multi-tenant.
* **Mensageria:** RabbitMQ utilizando **MassTransit** como abstração de alto nível para mensageria distribuída.

---

## 2. Pilares Arquiteturais (Instruções Obrigatórias para a IA)

O Agente de IA deve rejeitar qualquer código ou padrão que viole os cinco pilares abaixo:

### A. Monólito Modular (Modular Monolith)
1.  A aplicação deve residir em um único repositório e processo de execução, mas dividida rigidamente em módulos independentes (ex: `TenantManagement`, `EmailEngine`, `IdentityModule`).
2.  **Isolamento Absoluto:** Um módulo **nunca** pode acessar diretamente as tabelas ou o banco de dados de outro módulo.
3.  **Comunicação Inter-Módulos:**
    * *Síncrona (Apenas Leitura/Consultas):* Via interfaces públicas (`IModuleFacade` ou Queries mediadas).
    * *Asíncrona (Modificação de Estado/Efeitos Colaterais):* Via eventos de domínio publicados no broker de mensageria (RabbitMQ).

### B. Arquitetura Hexagonal (Ports & Adapters) dentro de cada Módulo
Cada módulo deve seguir estritamente a divisão hexagonal para isolar a lógica de negócio do ecossistema externo:


```

```text
AGENTS.md criado com sucesso.


```

```
              ┌─────────────────────────────────────┐
              │          Adapters (Inbound)         │
              │     Blazor UI / Controllers HTTP    │
              └──────────────────┬──────────────────┘
                                 │ (Chama)
                                 ▼
              ┌─────────────────────────────────────┐
              │            Ports (Inbound)          │
              │         Application Use Cases       │
              └──────────────────┬──────────────────┘
                                 │
                                 ▼
              ┌─────────────────────────────────────┐
              │             Domain Core             │
              │   Entities, Aggregates, Validations │
              └──────────────────┬──────────────────┘
                                 │
                                 ▼
              ┌─────────────────────────────────────┐
              │            Ports (Outbound)         │
              │    IRepository, IMessagingService   │
              └──────────────────┬──────────────────┘
                                 │ (Implementa)
                                 ▼
              ┌─────────────────────────────────────┐
              │          Adapters (Outbound)        │
              │       EF Core, RabbitMQ, Polly      │
              └─────────────────────────────────────┘

```

```

### C. Domain-Driven Design (DDD)
* **Camada de Domínio:** Deve ser pura (POCO), sem dependências de frameworks externos (incluindo EF Core ou annotations de validação).
* **Agregados e Entidades:** Toda modificação de estado deve passar pela raiz do Agregado, aplicando invariants e validações ricas. Não permitir entidades anêmicas.
* **Value Objects:** Usar extensivamente para tipos complexos (ex: `EmailAddress`, `ApiKey`, `DomainName`).

### D. Padrão Result (Result Pattern)
* **Proibido o uso de Exceções para Controle de Fluxo:** Exceções devem ser lançadas apenas para falhas catastróficas de infraestrutura.
* Toda operação de negócio, validação ou caso de uso deve retornar um objeto do tipo `Result<T>` indicando sucesso ou falha, acompanhado de erros estruturados.

```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException();
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado com falha.");

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    public static new Result<TValue> Failure(Error error) => new(default, false, error);
}

```

### E. Test-Driven Development (TDD)
* **Uso Obrigatório:** É estritamente obrigatório o uso de TDD para o desenvolvimento de todas as novas features e refatorações.
* O agente deve obrigatoriamente escrever os testes **antes** da implementação das features.
* **Ciclo Estrito:** Red (Escrever teste que falha) -> Green (Escrever código mínimo para passar) -> Refactor (Melhorar o código mantendo o teste verde).
* **Tipos de Testes Exigidos:**
  * *Testes Unitários (Domain):* Validar invariants das entidades e Value Objects sem mocks de banco.
  * *Testes de Integração (Adapters):* Validar repositórios reais contra um banco em memória ou Testcontainers, e publicação de mensagens.

### F. Comentários de Código e Documentação (<summary>)
* **Comentários Necessários:** Adicionar comentários explicativos no código quando a lógica de negócio for complexa ou não óbvia. Evitar comentários redundantes que apenas descrevem o que o código faz de forma direta.
* **Tag `<summary>` Obrigatória:** Todas as classes públicas, interfaces, métodos públicos expostos e endpoints da API devem ser devidamente documentados utilizando comentários XML contendo a tag `<summary>`.

### G. Integração OpenAPI + Scalar
* A aplicação da API (`Gateway.Bootstrapper`) deve possuir suporte nativo e completo para a geração de documentação de endpoints com OpenAPI, integrado ao **Scalar** para renderização de uma interface interativa e moderna de documentação.

### H. Documentação Viva (Living Documentation)
* **Atualização Contínua:** Toda e qualquer alteração arquitetural, novos endpoints, novos módulos, fluxos de integração ou regras de negócio relevantes devem ser obrigatoriamente documentados e atualizados nos arquivos de documentação do repositório (ex: `AGENTS.md`, `README.md`, ou arquivos de arquitetura específicos). A documentação deve evoluir lado a lado com a base de código, refletindo sempre a realidade atualizada do sistema.

---

## 3. Estrutura de Diretórios e Solução (.NET 10)

O agente deve estruturar a solução do Visual Studio / .NET CLI da seguinte forma:

```
src/
│
├── Gateway.Bootstrapper/         # Composição do Monólito, DI Central, Configurações de Middlewares (.NET 10)
│
├── Modules/
│   ├── Identity/
│   │   ├── Identity.Domain/       # Entidades de Usuário, Roles, Regras customizadas do Identity
│   │   ├── Identity.Application/  # Casos de uso de Login, MFA, Geração de Tokens JWT
│   │   └── Identity.Infrastructure/# Adapters: EF Core IdentityDbContext, Custom Stores
│   │
│   ├── TenantManagement/
│   │   ├── TenantManagement.Domain/ # Agregado Tenant, Domínios Vinculados, ApiKeys
│   │   ├── TenantManagement.Application/
│   │   └── TenantManagement.Infrastructure/
│   │
│   └── EmailEngine/
│       ├── EmailEngine.Domain/    # Agregado Email, Histórico, Configurações de Provedores
│       ├── EmailEngine.Application/ # Envio de e-mails, processamento de templates
│       └── EmailEngine.Infrastructure/ # Adapters: Integração com RabbitMQ, SMTP Client, SendGrid
│
└── Frontend/
    └── Gateway.Blazor/            # Aplicação Blazor (Client UI Admin e Dashboard de Clientes)

```

---

## 4. Estratégia Multi-Tenant e Isolamento de Banco de Dados

Para viabilizar a arquitetura SaaS comercializável de baixo custo operacional inicial, utilizaremos a abordagem **Single Database com Isolamento Lógico (Múltiplos Inquilinos na mesma tabela)**.

### Regras para o Agente de IA:

1. **Interface de Identificação:** Todas as entidades que pertencem a um cliente específico devem implementar `IMustHaveTenant`.
```csharp
public interface IMustHaveTenant
{
    public Guid TenantId { get; set; }
}

```


2. **Filtro Global do EF Core:** No `DbContext` de cada módulo, o agente deve configurar automaticamente o filtro global para injetar o `TenantId` resolvido por requisição do contexto atual (através do Header HTTP `X-API-KEY` ou Token JWT).
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configuração automática para todas as entidades IMustHaveTenant
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(CreateTenantFilterExpression(entityType.ClrType));
        }
    }
}

```


3. **Garantia de Isolamento no SaveChanges:** Sobrescrever o método `SaveChangesAsync` para garantir que nenhuma entidade seja salva sem o `TenantId` correto populado pelo sistema.

---

## 5. Arquitetura de Mensageria (RabbitMQ + MassTransit)

Para lidar com alta vazão de e-mails de múltiplos domínios simultâneos, a requisição HTTP da API apenas validará os dados e colocará uma mensagem na fila. O processamento pesado e o envio físico serão feitos de forma assíncrona.

### Configuração Padrão do MassTransit no Bootstrapper:

O agente deve implementar os contratos e consumidores (Consumers) separando o comando de envio do evento de resultado.

```csharp
// Contrato de Mensagem (Imutável)
public record SendEmailCommand(
    Guid TenantId,
    string To,
    string Subject,
    string Body,
    string SenderDomain,
    Dictionary<string, string> TemplateVariables
);

// Consumidor Assíncrono no módulo EmailEngine
public class SendEmailConsumer : IConsumer<SendEmailCommand>
{
    private readonly ISendEmailUseCase _sendEmailUseCase;

    public SendEmailConsumer(ISendEmailUseCase sendEmailUseCase)
    {
        _sendEmailUseCase = sendEmailUseCase;
    }

    public async Task Consume(ConsumeContext<SendEmailCommand> context)
    {
        // 1. Injeta o contexto do Tenant para execução segura
        TenantContext.SetCurrentTenant(context.Message.TenantId);

        // 2. Executa caso de uso através do padrão Result
        var result = await _sendEmailUseCase.ExecuteAsync(context.Message);

        if (result.IsFailure)
        {
            // Tratar retentativas automáticas ou mover para Dead Letter Queue (DLQ)
            throw new ProcessEmailException(result.Error.Message);
        }
    }
}

```

---

## 6. Checklist de Validação do Código (Pronto para Produção)

Antes de considerar qualquer tarefa de desenvolvimento concluída, o Agente de IA deve validar se o código atende aos seguintes requisitos estruturais:

* [ ] **Zero Anemic Domain:** As classes de domínio possuem construtores privados e métodos públicos de negócio baseados em regras reais?
* [ ] **Ports strictly defined:** A camada de aplicação se comunica com o banco de dados e mensageria apenas por interfaces (Ports)?
* [ ] **No Cross-Module Database Join:** Algum módulo está fazendo Join ou referência direta a tabelas de outros módulos? (Se sim, refatorar para comunicação baseada em eventos ou DTOs de integração).
* [ ] **Result Pattern applied:** Os controladores e use cases evitam blocos `try-catch` para controle de fluxo e utilizam `Result.Success` ou `Result.Failure`?
* [ ] **Identity isolation:** O Identity Framework está mapeando corretamente o escopo do usuário e vinculando-o a um `TenantId` corporativo?
* [ ] **TDD Mandatory & Compliant:** Foi obrigatoriamente utilizado TDD, escrevendo os testes antes do código? Existe uma suite de testes que cobre os caminhos felizes e infelizes do Use Case modificado?
* [ ] **Code Comments & <summary> Tags:** Comentários foram adicionados onde necessário e a tag `<summary>` foi utilizada em todas as classes, interfaces e métodos públicos?
* [ ] **OpenAPI & Scalar Integration:** A documentação da API está integrada e configurada com OpenAPI e Scalar no `Gateway.Bootstrapper`?
* [ ] **Living Documentation:** Toda alteração de arquitetura, fluxo, novos endpoints ou regras relevantes foi devidamente registrada e atualizada nas documentações do repositório (ex: `AGENTS.md`, `README.md`)?
* [ ] **Blazor UI Decoupled:** A interface Blazor está consumindo a API Web de forma limpa através de Handlers HTTP/HttpClient fortemente tipados, sem acoplamento com classes de domínio do backend?

---

## 7. Instruções Rápidas de Prompt para Inicialização

Quando você (Agente de IA) receber a ordem para criar uma nova funcionalidade, siga este protocolo de prompts internos:

1. *"Crie os cenários de teste xUnit na camada correspondente para a feature [NOME_DA_FEATURE] seguindo o padrão Given-When-Then."*
2. *"Escreva a entidade de domínio pura necessária para fazer esses testes compilarem e falharem (Fase Red)."*
3. *"Adicione a lógica mínima de negócio e regras de validação para passar os testes (Fase Green)."*
4. *"Implemente os Ports (Interfaces) de infraestrutura e crie os Adapters usando EF Core e MassTransit (Fase Refactor/Infra)."*

---

## 8. Integração com MCP Stitch
* **Ferramenta de Apoio:** Este projeto utiliza o MCP Server Stitch para integrações de ferramentas adicionais de apoio ao desenvolvimento. O console do projeto no Stitch correspondente está disponível em: [Stitch Project 13296040073188886497](https://stitch.withgoogle.com/projects/13296040073188886497).

