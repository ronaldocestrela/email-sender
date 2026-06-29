# Email Sender: Sistema de Envio de E-mails Multi-Tenant

Este repositório contém a especificação e a base de código do **Sistema de Envio de E-mails Multi-Tenant**, uma solução robusta estruturada sob o padrão de **Monólito Modular** com **Arquitetura Hexagonal (Ports & Adapters)** por módulo, preparada para escala, venda como SaaS (White-Label) e baixo custo operacional inicial.

---

## 🚀 Tecnologias Utilizadas (Core Stack)

* **Backend:** .NET 10 (ASP.NET Core Web API) com OpenAPI e documentação Scalar integrada.
* **Frontend:** Blazor (Interactive Auto ou WebAssembly) para painel administrativo.
* **Banco de Dados:** SQL Server mapeado via EF Core 10 com isolamento lógico (`TenantId`).
* **Mensageria:** RabbitMQ utilizando a abstração do MassTransit.
* **Autenticação:** Microsoft Identity Framework adaptado para multi-tenancy.

---

## 🏛️ Pilares Arquiteturais

A arquitetura do projeto segue princípios modernos de design de software:

1. **Monólito Modular (Modular Monolith):** O projeto executa sob um único processo, mas é dividido rigidamente em módulos isolados (`TenantManagement`, `EmailEngine`, `Identity`). O acesso direto ao banco de dados entre módulos é estritamente proibido.
2. **Arquitetura Hexagonal (Ports & Adapters):** Cada módulo desacopla suas regras de negócio (Domain Core) e casos de uso (Application Ports) do ecossistema externo (Adapters de infraestrutura como EF Core, RabbitMQ, etc.).
3. **Domain-Driven Design (DDD):** Domínio rico com entidades POCO puras, invariants bem definidos e Value Objects para tipos complexos.
4. **Result Pattern:** Substituição de exceções para fluxo de controle de regras de negócio, retornando status estruturados de sucesso ou falha (`Result<T>`).
5. **Test-Driven Development (TDD):** Uso obrigatório do ciclo Red-Green-Refactor para desenvolvimento de novas funcionalidades e validação contínua.
6. **Estratégia Multi-Tenant:** Banco de dados compartilhado com isolamento lógico global (Filtros Globais de Consulta do EF Core) baseado no contexto de requisição.

---

## 📂 Estrutura de Diretórios

```text
src/
│
├── Gateway.Bootstrapper/         # Inicialização do Monólito, injeção centralizada e middleware de Tenant
│
├── Modules/
│   ├── Identity/                 # Autenticação de usuários, MFA, roles e tokens JWT
│   ├── TenantManagement/         # Gerenciamento de Inquilinos, ApiKeys e domínios parceiros
│   └── EmailEngine/              # Filas de envio, integração SMTP/Providers e histórico de disparo
│
└── Frontend/
    └── Gateway.Blazor/            # Frontend em Blazor para gestão de tenants e histórico
```

---

## 📖 Documentação Adicional

Para entender as diretrizes de desenvolvimento do time, arquitetura e o plano de ação, acesse os guias a seguir:

* **[ARCHITECTURE.md](file:///home/rony/LPR/email-sender/ARCHITECTURE.md) (Arquitetura e Guia de Desenvolvimento):** Detalhes das camadas hexagonais, matriz de referências entre projetos, estratégia de multi-tenancy e fluxo de mensageria assíncrona.
* **[AGENTS.md](file:///home/rony/LPR/email-sender/AGENTS.md) (Diretrizes do Agente):** Regras de desenvolvimento, padrões de codificação (como o uso obrigatório de TDD, XML `<summary>` e Scalar) e o checklist de validação de PRs.
* **[Roadmap.md](file:///home/rony/LPR/email-sender/Roadmap.md) (Roadmap de Execução):** Sequenciamento das fases de desenvolvimento, do setup inicial até o roteiro completo de testes manuais E2E.

---

## 🔗 Integração com MCP Stitch

Este projeto utiliza o servidor MCP **Stitch** para integrações de ferramentas adicionais e controle de desenvolvimento.
* O console do projeto no Stitch está disponível em: [Stitch Project 13296040073188886497](https://stitch.withgoogle.com/projects/13296040073188886497).

---

## 🛠️ Como Iniciar (Pré-requisitos Locais)

1. **Docker Desktop/CLI:** Necessário para subir a infraestrutura de apoio (SQL Server, RabbitMQ, Mailpit).
2. **.NET 10 SDK:** SDK do .NET instalado na máquina de desenvolvimento.
3. **Inicialização do Ambiente:**
   ```bash
   docker compose up -d
   ```
4. **Execução das Migrações e Inicialização do Backend:**
   Consulte os roteiros específicos no arquivo [Roadmap.md](file:///home/rony/LPR/email-sender/Roadmap.md).
