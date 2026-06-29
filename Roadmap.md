# Roadmap de Desenvolvimento: Sistema de Envio de E-mails Multi-Tenant

Este roadmap define as etapas e subetapas para a construção de ponta a ponta do sistema de envio de e-mails, abrangendo desde a configuração da infraestrutura local, backend (.NET 10) e banco de dados, até a interface administrativa em Blazor e testes manuais de validação.

> [!NOTE]
> Este projeto utiliza o MCP Server Stitch para integrações de ferramentas adicionais de apoio ao desenvolvimento. O console do projeto no Stitch correspondente está disponível em: [Stitch Project 13296040073188886497](https://stitch.withgoogle.com/projects/13296040073188886497).

---

## Fase 1: Setup do Ambiente e Estrutura da Solução

### Etapa 1.1: Inicialização da Estrutura de Diretórios e Solução
- [x] Criar a solução principal do .NET 10 (`EmailSender.slnx`) na raiz `src/`.
- [x] Criar os projetos do módulo `TenantManagement`:
  - `Modules/TenantManagement/TenantManagement.Domain` (Class Library)
  - `Modules/TenantManagement/TenantManagement.Application` (Class Library)
  - `Modules/TenantManagement/TenantManagement.Infrastructure` (Class Library)
- [x] Criar os projetos do módulo `Identity`:
  - `Modules/Identity/Identity.Domain` (Class Library)
  - `Modules/Identity/Identity.Application` (Class Library)
  - `Modules/Identity/Identity.Infrastructure` (Class Library)
- [x] Criar os projetos do módulo `EmailEngine`:
  - `Modules/EmailEngine/EmailEngine.Domain` (Class Library)
  - `Modules/EmailEngine/EmailEngine.Application` (Class Library)
  - `Modules/EmailEngine/EmailEngine.Infrastructure` (Class Library)
- [x] Criar o Gateway centralizador `Gateway.Bootstrapper` (ASP.NET Core Web API).
- [x] Criar o Frontend `Gateway.Blazor` (Blazor WebAssembly ou Interactive Auto).
- [x] Vincular todos os projetos criados à solução central.

### Etapa 1.2: Configuração do Ambiente de Apoio (Docker)
- [x] Criar um arquivo `docker-compose.yml` na raiz do projeto contendo:
  - Banco de Dados SQL Server.
  - Broker de Mensageria (RabbitMQ com painel de gerenciamento habilitado).
  - Ferramenta de teste de e-mail local (ex: Mailpit ou Mailhog) para capturar os envios SMTP sem disparar e-mails reais.

---

## Fase 2: Implementação do Core e Domínio (Foco em TDD)

### Etapa 2.1: Módulo Tenant Management (TDD)
- [x] Criar o projeto de testes unitários `TenantManagement.Domain.Tests`.
- [x] **TDD - Ciclo Red/Green/Refactor:**
  - [x] Implementar testes para a criação do Agregado `Tenant` e validação de invariants.
  - [x] Implementar testes para o Value Object `ApiKey` (geração segura, formato e hash).
  - [x] Implementar testes para validação de domínios vinculados (`DomainName`).
- [x] Codificar o domínio puramente POCO no `TenantManagement.Domain` para satisfazer os testes.
- [x] Definir os Ports de saída (ex: `ITenantRepository`).

### Etapa 2.2: Módulo Identity e Segurança (TDD)
- [x] Criar o projeto de testes `Identity.Domain.Tests`.
- [x] **TDD - Ciclo Red/Green/Refactor:**
  - [x] Implementar testes de criação de `User` vinculando ao `TenantId`.
  - [x] Implementar testes de validação de papéis (`Roles`) e políticas multi-tenant.
- [x] Desenhar os casos de uso no `Identity.Application`:
  - Login e geração de Tokens JWT contendo as Claims de `TenantId` e `Role`.
  - Geração e verificação de Multi-Factor Authentication (MFA).

---

## Fase 3: Camada de Persistência e Isolamento Multi-Tenant

### Etapa 3.1: Configuração do EF Core e Migrations
- [ ] Instalar o Entity Framework Core 10 nos projetos de infraestrutura correspondentes.
- [ ] Configurar a interface `IMustHaveTenant` e mapeamento global do filtro de consulta no `DbContext` compartilhado ou específico de cada módulo.
- [ ] Sobrescrever o método `SaveChangesAsync` nos DbContexts para autodefinir e aplicar o `TenantId` do contexto atual para novas entidades.
- [ ] Gerar as primeiras migrations dos módulos e configurar o mecanismo de execução de migrations na inicialização do Bootstrapper.

### Etapa 3.2: Middleware de Resolução de Tenant
- [ ] Desenvolver um middleware HTTP no `Gateway.Bootstrapper` para identificar o Tenant atual por:
  - Header HTTP `X-API-KEY` (para integrações externas).
  - Claim `TenantId` extraída do Token JWT (para usuários do Blazor Dashboard).
- [ ] Criar uma classe com escopo de requisição (`TenantContext`) para armazenar e expor o `TenantId` resolvido.

---

## Fase 4: Engine de E-mail e Mensageria Assíncrona

### Etapa 4.1: Mensageria com MassTransit e RabbitMQ
- [ ] Criar os contratos de mensagem (ex: `SendEmailCommand`) e registrar o MassTransit no `Gateway.Bootstrapper`.
- [ ] Configurar a conexão com o container do RabbitMQ.
- [ ] Implementar o `SendEmailConsumer` no `EmailEngine.Infrastructure` que consome as mensagens da fila.
- [ ] Garantir que o consumidor execute injetando corretamente o `TenantId` no contexto da thread para fins de isolamento de banco.

### Etapa 4.2: Envio Físico de E-mail (Adapters)
- [ ] Criar interfaces de envio no `EmailEngine.Application` (`IEmailSender`).
- [ ] Implementar o Adapter SMTP apontando para o container Mailpit local.
- [ ] Implementar o Adapter de produção (SendGrid ou AWS SES) configurável via variáveis de ambiente.
- [ ] Implementar persistência de logs e histórico de envio no `EmailEngine.Domain`.

---

## Fase 5: Exposição da API Gateway e Documentação

### Etapa 5.1: Criação dos Endpoints HTTP
- [ ] Desenvolver controllers HTTP ou endpoints mínimos (Minimal APIs) para:
  - Autenticação e MFA.
  - Cadastro de Tenants e geração de ApiKeys.
  - Solicitação assíncrona de disparo de e-mail (envia o comando para a fila).
- [ ] Garantir que todas as APIs retornem resultados padronizados usando o `Result Pattern`.

### Etapa 5.2: OpenAPI + Scalar
- [ ] Adicionar suporte ao OpenAPI (.NET 10 OpenAPI Generator/Swashbuckle/Microsoft.AspNetCore.OpenApi).
- [ ] Configurar a renderização interativa do **Scalar** no pipeline de middlewares para facilitar testes manuais.
- [ ] Documentar todos os endpoints públicos com a tag `<summary>`.

---

## Fase 6: Frontend Blazor (Gateway.Blazor)

### Etapa 6.1: Setup da Interface e Comunicação
- [ ] Inicializar o projeto Blazor e desacoplar das referências diretas das classes de backend.
- [ ] Implementar um HttpClient customizado (ou HTTP Handler) que anexa automaticamente o Token JWT ou a `ApiKey` nos cabeçalhos das requisições.
- [ ] Implementar componentes básicos de layout utilizando CSS moderno e customizado (responsividade, paleta de cores elegantes e transições suaves).

### Etapa 6.2: Construção das Páginas Administrativas
- [ ] Tela de Login / Cadastro e configuração inicial do MFA.
- [ ] Dashboard Principal:
  - Listagem e criação de novos Tenants (Exclusivo Admin Geral).
  - Visualização de logs e histórico de disparos de e-mail filtrados por Tenant do usuário logado.
  - Geração e revogação de chaves de API (`ApiKeys`).

---

## Fase 7: Homologação e Teste Manual E2E (Ponta a Ponta)

### Etapa 7.1: Preparação do Ambiente de Teste Manual
- [ ] Iniciar todos os containers locais (`docker-compose up -d`).
- [ ] Executar migrações do banco de dados.
- [ ] Criar um script inicial (Seeder) para gerar o primeiro Tenant Administrativo e uma ApiKey de teste.

### Etapa 7.2: Roteiro do Teste Manual Ponta a Ponta
- [ ] **Teste 1: Acesso ao Scalar e Validação de Tenant**
  - Acessar a rota da documentação Scalar (`/scalar/v1` ou equivalente).
  - Tentar disparar um e-mail sem API Key e certificar que retorna `401 Unauthorized` ou erro de negócio via Result Pattern.
- [ ] **Teste 2: Disparo via API**
  - Chamar o endpoint de envio enviando um payload de e-mail com a API Key de teste configurada no header `X-API-KEY`.
  - Confirmar que a API responde instantaneamente sucesso (e-mail enfileirado) com o ID do envio.
- [ ] **Teste 3: Processamento Assíncrono**
  - Acessar o console do RabbitMQ e verificar o tráfego da mensagem de envio de e-mail.
  - Acessar a interface do Mailpit local (`http://localhost:8025` ou equivalente) e confirmar se o e-mail mockado foi recebido perfeitamente com os dados fornecidos.
- [ ] **Teste 4: Interface Blazor**
  - Acessar o painel Blazor, logar com o usuário de testes.
  - Navegar até o histórico de e-mails e conferir se o e-mail disparado via API está registrado no histórico daquele Tenant.
  - Tentar acessar a URL ou histórico de outro Tenant fictício e garantir o isolamento lógico das informações.
