# Módulo Email Engine (EmailEngine)

O módulo **Email Engine** é responsável por receber as solicitações assíncronas de disparo de e-mails, resolver as credenciais específicas de provedores por inquilino, executar o envio físico utilizando o provedor correto e auditar o histórico de disparos no banco de dados. Ele reside sob o diretório [Modules/EmailEngine](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine).

---

## 1. Arquitetura do Módulo

O módulo é integrado de forma assíncrona ao broker de mensageria:

```text
                  MassTransit Fila (RabbitMQ)
                              │
                              ▼
                     [SendEmailConsumer]
                              │ (Chama)
                              ▼
                ┌───────────────────────────┐
                │   EmailEngine.Application │
                │        (Use Cases)        │
                │     SendEmailUseCase      │
                └──────┬─────────────┬──────┘
                       │ (Chama)     │ (Implementa)
                       ▼             ▼
  ┌────────────────────────┐     ┌─────────────────────────────┐
  │   EmailEngine.Domain   │     │  EmailEngine.Application.   │
  │ (Entities & Aggregates)│     │            Ports            │
  │ EmailHistory, Settings │     │ IEmailSender, repositories  │
  └────────────────────────┘     └─────────────┬───────────────┘
                                               │ (Implementa)
                                               ▼
                                 ┌─────────────────────────────┐
                                 │  EmailEngine.Infrastructure │
                                 │    (Smtp, SendGrid, EF)     │
                                 └─────────────────────────────┘
```

---

## 2. Camada de Domínio (`EmailEngine.Domain`)

* **EmailProviderSettings (Entity):** [EmailProviderSettings.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Domain/Entities/EmailProviderSettings.cs)
  * Configurações customizadas do remetente e servidor por Tenant (SMTP ou SendGrid API).
  * Invariantes incluem validação estrutural do e-mail do remetente e validação específica baseada no tipo de provedor (ex: porta SMTP e host obrigatórios; ApiKey obrigatória no SendGrid).
* **EmailHistory (Aggregate Root):** [EmailHistory.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Domain/Aggregates/EmailHistory.cs)
  * Histórico de auditoria contendo dados estruturados de disparo (destinatário, assunto, corpo, domínio remetente, data/hora, status de sucesso e mensagem de erro).
  * Ambos herdam de `IMustHaveTenant` para isolamento de dados por Tenant.

---

## 3. Camada de Aplicação (`EmailEngine.Application`)

* **Casos de Uso (Use Cases):**
  * `SendEmailUseCase`: [SendEmailUseCase.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Application/UseCases/SendEmailUseCase.cs)
    * Processa o comando assíncrono recebido.
    * Busca credenciais customizadas em `IEmailProviderSettingsRepository`.
    * Normaliza variáveis de template informadas em `TemplateVariables` substituindo padrões `{{key}}` no corpo e assunto.
    * Delega o envio físico para o port `IEmailSender`.
    * Grava o histórico de auditoria via `IEmailHistoryRepository`.

* **Portas de Saída (Ports):**
  * [IEmailSender.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Application/Ports/IEmailSender.cs): Abstração para envio físico.
  * [IEmailHistoryRepository.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Application/Ports/IEmailHistoryRepository.cs): Persistência de logs.
  * [IEmailProviderSettingsRepository.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Application/Ports/IEmailProviderSettingsRepository.cs): Persistência de credenciais.

---

## 4. Camada de Infraestrutura (`EmailEngine.Infrastructure`)

* **SmtpEmailSender (MailKit):** [SmtpEmailSender.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Infrastructure/EmailSenders/SmtpEmailSender.cs)
  * Adapter concreto utilizando a biblioteca **MailKit**. Em desenvolvimento, ignora validação de certificados SSL para facilitar testes locais (ex: Mailpit).
* **SendGridEmailSender (HTTP API):** [SendGridEmailSender.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Infrastructure/EmailSenders/SendGridEmailSender.cs)
  * Adapter leve efetuando requisições REST autenticadas via `HttpClient` direto na API v3 do SendGrid, evitando acoplamento com SDKs.
* **CompositeEmailSender (Roteador Dinâmico):** [CompositeEmailSender.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Infrastructure/EmailSenders/CompositeEmailSender.cs)
  * Orquestra qual adapter chamar. Se o Tenant possuir credenciais customizadas de SMTP ou SendGrid, utiliza-as. Caso contrário, aplica as configurações globais do sistema presentes no `appsettings.json`.
* **MassTransit Consumer (SendEmailConsumer):** [SendEmailConsumer.cs](file:///home/rony/LPR/email-sender/src/Modules/EmailEngine/EmailEngine.Infrastructure/Consumers/SendEmailConsumer.cs)
  * Escuta o comando de envio da fila do RabbitMQ.
  * **Configuração crítica de Contexto:** Como o consumidor executa em uma thread de background independente, ele deve obrigatoriamente chamar a interface `ITenantSetter` passando o `TenantId` da mensagem recebida da fila. Isso garante que o DbContext do Entity Framework resolva a expressão correta do filtro lógico de isolamento de banco durante toda a execução da thread de envio e gravação de histórico.

---

## 5. Manutenção e Extensibilidade

1. **Adicionar Provedor Novo (Ex: AWS SES):**
   * Adicionar o tipo na enum `EmailProviderType` (Domínio).
   * Implementar `AwsSesEmailSender : IEmailSender` na infraestrutura.
   * Ajustar o resolver em `CompositeEmailSender` para lidar com a nova enum.
2. **Alterar Mecanismo de Template:** O motor de template utiliza substituição de string direta `Replace("{{key}}", value)`. Caso deseje utilizar Razor, Handlebars ou Liquid, isso deve ser integrado e encapsulado na etapa de normalização dentro do `SendEmailUseCase` de aplicação.
