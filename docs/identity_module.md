# Módulo Identity e Segurança (Identity)

Este módulo é responsável por gerenciar credenciais de usuários, controle de acesso baseado em cargos (RBAC) e o segundo fator de autenticação (MFA - TOTP). Ele reside sob o diretório [Modules/Identity](file:///home/rony/LPR/email-sender/src/Modules/Identity).

---

## 1. Arquitetura do Módulo

O módulo segue a **Arquitetura Hexagonal** (Ports & Adapters) integrada a princípios de **Domain-Driven Design (DDD)**:

```text
               ┌──────────────────────────────────────────────────┐
               │                Identity.Application              │
               │                   (Use Cases)                    │
               │            LoginUseCase, MfaUseCase          │
               └─────────┬──────────────────────────────┬─────────┘
                         │ (Chama)                      │ (Implementa)
                         ▼                              ▼
    ┌───────────────────────────┐          ┌──────────────────────────┐
    │      Identity.Domain      │          │ Identity.Application.Ports│
    │  (Entities & Aggregates)  │          │   (Outbound Interfaces)  │
    │            User           │          │ IUserRepository,         │
    └───────────────────────────┘          │ IMfaService, ITokenService│
                                           └────────────┬─────────────┘
                                                        │ (Implementa)
                                                        ▼
                                           ┌──────────────────────────┐
                                           │  Identity.Infrastructure │
                                           │  (EF Core Repositories,  │
                                           │   PBKDF2, TOTP Security) │
                                           └──────────────────────────┘
```

---

## 2. Camada de Domínio (`Identity.Domain`)

A camada de domínio é puramente orientada a objetos (POCO), contendo entidades ricas que validam suas próprias invariantes no construtor e em seus métodos de negócio.

* **User (Aggregate Root):** [User.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Domain/Aggregates/User.cs)
  * Representa o usuário corporativo. Herda de `IMustHaveTenant` para isolamento lógico por Tenant.
  * **Validações Invariantes:**
    * Estrutura de e-mail (regex padrão RFC).
    * Senha não vazia ou nula.
    * Cargos válidos (Admin, User).
  * **MFA (TOTP) state machine:**
    * `EnableMfa(string secret)`: Valida o segredo em formato Base32 e altera o estado para habilitado.
    * `DisableMfa()`: Desativa as flags e limpa o segredo TOTP.

---

## 3. Camada de Aplicação (`Identity.Application`)

Define os casos de uso do negócio e as portas de entrada e saída.

* **Casos de Uso (Use Cases):**
  * `LoginUseCase`: [LoginUseCase.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Application/UseCases/LoginUseCase.cs)
    * Efetua validação inicial.
    * Busca o usuário e valida o hash da senha.
    * Se o MFA estiver ativo, valida o código TOTP de 6 dígitos.
    * Gera e retorna o Token JWT via `ITokenService`.
  * `MfaUseCase`: [MfaUseCase.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Application/UseCases/MfaUseCase.cs)
    * `SetupMfaAsync`: Gera um segredo Base32 randômico de 80 bits e monta a URI do QR Code.
    * `ConfirmMfaAsync`: Valida o primeiro código gerado no aplicativo autenticador do usuário e confirma a ativação no perfil.
    * `DisableMfaAsync`: Desabilita a verificação.

* **Portas de Saída (Ports):**
  * [IUserRepository.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Application/Ports/IUserRepository.cs): Port de acesso ao banco.
  * [IPasswordHasher.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Application/Ports/IPasswordHasher.cs): Abstração para geração de hashes criptográficos de senhas.
  * [IMfaService.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Application/Ports/IMfaService.cs): Abstração para algoritmos TOTP.
  * [ITokenService.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Application/Ports/ITokenService.cs): Abstração para emissão de JWT.

---

## 4. Camada de Infraestrutura (`Identity.Infrastructure`)

Fornece as implementações concretas dos adapters tecnológicos.

* **Persistence (EF Core):** [UserRepository.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Infrastructure/Persistence/Repositories/UserRepository.cs)
  * **Isolamento Multi-Tenant Bypassed:** Durante os fluxos de login e MFA, o contexto `TenantId` da requisição HTTP ainda não foi resolvido. Portanto, para buscar o usuário no banco por e-mail ou por ID, o repositório usa obrigatoriamente o método `.IgnoreQueryFilters()`. O isolamento é mantido via validação criptográfica (senha/MFA).
* **PBKDF2 Password Hasher:** [PasswordHasher.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Infrastructure/Security/PasswordHasher.cs)
  * Utiliza o algoritmo **PBKDF2 (SHA256)** com Salt aleatório de 128 bits e 100.000 iterações.
  * A comparação é feita utilizando o método `CryptographicOperations.FixedTimeEquals` para mitigar ataques de temporização (Timing Attacks).
* **TOTP MFA Service:** [MfaService.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Infrastructure/Security/MfaService.cs)
  * Implementação nativa da **RFC 6238**.
  * Codificador/Decodificador Base32 nativo para manipulação de segredos.
  * Tolerância de compensação de tempo (clock-drift) de +/- 1 ciclo de 30 segundos (valida a janela anterior, atual e posterior).
* **Token JWT Service:** [TokenService.cs](file:///home/rony/LPR/email-sender/src/Modules/Identity/Identity.Infrastructure/Security/TokenService.cs)
  * Gera tokens JWT contendo as claims de `TenantId` do usuário, email e cargos (`Role`). A chave simétrica é recuperada do `JwtSettings:Secret` de configuração.

---

## 5. Manutenção e Extensibilidade

Ao refatorar ou dar manutenção neste módulo:
1. **Novos Atributos de Usuário:** Devem ser inseridos no construtor privado e métodos de criação do aggregate `User` na camada de Domínio, refletidos nos mapeamentos de tabela do `IdentityDbContext` e cobertos por testes de unidade.
2. **Substituição de Algoritmo de Hashing:** Basta implementar a interface `IPasswordHasher` em um novo adapter (ex: `Argon2PasswordHasher`) e alterar seu registro DI no `Program.cs`.
