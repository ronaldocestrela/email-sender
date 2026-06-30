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

* **[ARCHITECTURE.md](ARCHITECTURE.md) (Arquitetura e Guia de Desenvolvimento):** Detalhes das camadas hexagonais, matriz de referências entre projetos, estratégia de multi-tenancy e fluxo de mensageria assíncrona.
* **[AGENTS.md](AGENTS.md) (Diretrizes do Agente):** Regras de desenvolvimento, padrões de codificação (como o uso obrigatório de TDD, XML `<summary>` e Scalar) e o checklist de validação de PRs.
* **[Roadmap.md](Roadmap.md) (Roadmap de Execução):** Sequenciamento das fases de desenvolvimento, do setup inicial até o roteiro completo de testes manuais E2E.

---

## 🔗 Integração com MCP Stitch

Este projeto utiliza o servidor MCP **Stitch** para integrações de ferramentas adicionais e controle de desenvolvimento.
* O console do projeto no Stitch está disponível em: [Stitch Project 13296040073188886497](https://stitch.withgoogle.com/projects/13296040073188886497).

---

## 🛠️ Como Iniciar e Rodar Localmente (Passo a Passo)

### 1. Pré-requisitos
* **Docker Desktop / Docker CLI**
* **.NET 10 SDK** instalado localmente.

### 2. Inicializar a Infraestrutura (Containers)
No diretório raiz do projeto, suba os containers locais (SQL Server, RabbitMQ e Mailpit):
```bash
docker compose up -d
```

### 3. Executar o Backend (Gateway.Bootstrapper)
Com os containers rodando, inicie a API do backend:
```bash
dotnet run --project src/Gateway.Bootstrapper/Gateway.Bootstrapper.csproj
```
> [!NOTE]
> No primeiro startup, o Entity Framework Core aplicará as migrations de banco automaticamente. O banco de dados vazio disparará o `DatabaseSeeder` que gera os dados de teste iniciais. **Copie a Chave de API impressa no console do backend** em nível de `Warning` para testes de disparos diretos.

### 4. Executar o Frontend (Gateway.Blazor)
Em outro terminal, execute o painel administrativo Blazor:
```bash
dotnet run --project src/Frontend/Gateway.Blazor/Gateway.Blazor.csproj
```

---

## 🔗 Endpoints e Painéis de Acesso Local

Após iniciar todos os projetos, os seguintes painéis e serviços estarão acessíveis:

* **Painel Blazor (Frontend Admin):** [http://localhost:5139](http://localhost:5139)
* **Documentação Scalar (Playground Backend):** [http://localhost:5090/scalar/v1](http://localhost:5090/scalar/v1)
* **Mailpit (Visualizador Mock de E-mails):** [http://localhost:8025](http://localhost:8025)
* **RabbitMQ Dashboard (Filas):** [http://localhost:15672](http://localhost:15672) (credenciais `guest`/`guest`)

---

## 🔑 Credenciais de Teste (Geradas pelo Seeder)

* **Usuário Administrador (Login Blazor):**
  * **E-mail:** `admin@admintent.com`
  * **Senha:** `Admin@123`
  * **Papel:** `Admin` (MFA desativado inicialmente)
* **Chave de API:** Impressa nos logs do console do Backend (prefixo `es_live_...`). Utilize-a no cabeçalho HTTP `X-API-KEY` para testar disparos via `/api/emails/send`.

