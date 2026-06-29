# Arquitetura e Guia de Desenvolvimento - EmailSender

Este documento detalha o design arquitetural, o isolamento multi-tenant, o fluxo de mensageria e as instruções de desenvolvimento para o sistema **EmailSender**.

---

## 1. Visão Geral da Arquitetura

O sistema é construído como um **Monólito Modular** dividido em módulos rígidos e fracamente acoplados, usando os conceitos da **Arquitetura Hexagonal (Ports & Adapters)** em cada módulo.

```text
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

### 1.1 Módulos Rígidos
O sistema é composto por três módulos isolados sob o diretório `src/Modules/`:
1. **TenantManagement:** Cadastro e controle de Inquilinos (Tenants), domínios associados e chaves de API (`ApiKeys`).
2. **Identity:** Autenticação, Roles de segurança e Multi-Factor Authentication (MFA).
3. **EmailEngine:** Recebimento e processamento assíncrono de e-mails, gerenciamento de provedores (SMTP, SendGrid, etc.) e auditoria de envio.

---

## 2. Divisão de Camadas (Arquitetura Hexagonal)

Cada módulo é dividido em três projetos .NET 10:

### A. Camada de Domínio (`.Domain`)
* **POCO Puro:** Livre de dependências de frameworks externos (ex: EF Core ou annotations de validação).
* Contém Entidades, Agregados (que gerenciam suas próprias invariantes), Value Objects (`ApiKey`, `DomainName`, `EmailAddress`) e Eventos de Domínio.

### B. Camada de Aplicação (`.Application`)
* Contém a lógica de orquestração de negócios (Casos de Uso) e os **Ports de Entrada** (Interfaces de Casos de Uso) e **Ports de Saída** (Interfaces de Repositórios, Gateways de envio, etc.).
* Depende apenas da camada de Domínio.

### C. Camada de Infraestrutura (`.Infrastructure`)
* Contém os **Adapters de Saída** (ex: EF Core DbContext, Repositórios concretos, integrações SMTP, drivers RabbitMQ).
* Depende das camadas de Aplicação e Domínio.

---

## 3. Matriz de Dependência de Projetos (.slnx)

A solução [EmailSender.slnx](file:///home/rony/LPR/email-sender/src/EmailSender.slnx) gerencia os 11 projetos com as seguintes regras de referências:

```text
                               ┌──────────────────────┐
                               │ Gateway.Bootstrapper │
                               └──────────┬───────────┘
                                          │ (Compõe / DI)
                                          ▼
   ┌────────────────────────────────────────────────────────────────────────┐
   │                                Módulos                                 │
   │                                                                        │
   │   ┌───────────────────────────┐         ┌───────────────────────────┐  │
   │   │   *.Infrastructure        │────────>│   *.Application           │  │
   │   └───────────────────────────┘         └─────────────┬─────────────┘  │
   │                                                       │                │
   │                                                       ▼                │
   │                                         ┌───────────────────────────┐  │
   │                                         │   *.Domain (POCO Puro)    │  │
   │                                         └───────────────────────────┘  │
   └────────────────────────────────────────────────────────────────────────┘

                               ┌──────────────────────┐
                               │    Gateway.Blazor    │
                               └──────────────────────┘
                                (Decoupled - HTTP Only)
```

* **Gateway.Blazor:** Não possui nenhuma referência a outros projetos de backend da solução. Toda a comunicação ocorre via HttpClient direcionada ao Bootstrapper.
* **Gateway.Bootstrapper:** Referencia a `Application` e `Infrastructure` de cada módulo para fazer a injeção de dependência e expor os Controllers HTTP.

---

## 4. Estratégia Multi-Tenant

O EmailSender utiliza a abordagem **Single Database com Isolamento Lógico (Múltiplos inquilinos na mesma tabela)**.

1. **IMustHaveTenant:** Interface que define a coluna de tenant:
   ```csharp
   public interface IMustHaveTenant
   {
       public Guid TenantId { get; set; }
   }
   ```
2. **Filtro Global do EF Core:** Configurado no `OnModelCreating` do DbContext de cada módulo para barrar consultas a dados que não pertençam ao Tenant contextualizado na requisição.
3. **Resolução do Tenant:** Feita via Middleware no Bootstrapper. O TenantId é extraído:
   * Do Header `X-API-KEY` (para integrações externas de envio).
   * Do Token JWT (para navegação e operações no painel Blazor).

---

## 5. Fluxo de Envio Assíncrono (Mensageria)

O envio de e-mails segue um padrão assíncrono baseado em filas para garantir alta vazão e resiliência:

```text
 [API Client] ──(HTTP POST)──> [Gateway.Bootstrapper]
                                        │
                               (Enfileira comando)
                                        ▼
                                 [RabbitMQ Queue]
                                        │
                             (Consumido Assincronamente)
                                        ▼
    [Mailpit / SMTP] <──(SMTP)── [SendEmailConsumer]
```

1. A API recebe a solicitação de envio, valida o request, resolve o Tenant atual e publica uma mensagem `SendEmailCommand` no RabbitMQ via **MassTransit**.
2. A API retorna imediatamente sucesso (`202 Accepted`) com o ID da mensagem.
3. O `SendEmailConsumer` (no módulo `EmailEngine.Infrastructure`) consome a mensagem da fila, ativa o contexto do Tenant e processa o envio real através do provedor configurado.

---

## 6. Ambiente Docker de Apoio

Os serviços de apoio local são definidos no [docker-compose.yml](file:///home/rony/LPR/email-sender/docker-compose.yml):

* **SQL Server:** Porta `1433` (Senha SA: `SqlServerPwd123!`)
* **RabbitMQ Dashboard:** `http://localhost:15672` (Credenciais: `guest`/`guest`)
* **Mailpit Dashboard:** `http://localhost:8025` (SMTP na porta `1025`)
