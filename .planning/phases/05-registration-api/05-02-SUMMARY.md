---
phase: 05-registration-api
plan: "02"
subsystem: backend-infrastructure
tags: [ef-core, postgresql, keycloak-admin-sdk, repository-pattern, dependency-injection]
dependency_graph:
  requires:
    - "05-01: TDD stubs (RED) — IClientRepository interface, RegisterClientCommandHandler stub"
    - "03: Domain layer — Client aggregate, value objects, IClientRepository"
    - "04: Observability — Serilog + OTel already wired in API"
  provides:
    - "AppDbContext com DbSet<Client> e mapeamento value objects via HasConversion"
    - "ClientRepository com todos os métodos IClientRepository incluindo DeleteAsync (REG-06)"
    - "KeycloakUserService implementando IKeycloakUserService via IKeycloakUserClient"
    - "AddInfrastructure() extension pronta para Program.cs"
    - "IKeycloakUserService na camada Application (sem dependência de SDK)"
  affects:
    - "05-03: Controller depende de AddInfrastructure() e IKeycloakUserService"
    - "05-04: IdempotencyFilter usa IDistributedCache que AddInfrastructure() depende"
tech_stack:
  added:
    - "Microsoft.EntityFrameworkCore 10.0.5"
    - "Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1"
    - "Microsoft.EntityFrameworkCore.Design 10.0.5"
    - "Keycloak.AuthServices.Sdk 2.9.0"
    - "Duende.AccessTokenManagement 4.2.0"
  patterns:
    - "IEntityTypeConfiguration<T> para separar mapeamento EF Core"
    - "HasConversion com value objects imutáveis (sealed record)"
    - "Partial unique indexes via HasFilter para colunas nullable (CPF/CNPJ)"
    - "ClientCredentialsClientName.Parse() + ClientId.Parse() + ClientSecret.Parse() para Duende 4.x strongly-typed values"
    - "IKeycloakUserClient (não IKeycloakClient) como interface correta do Keycloak.AuthServices.Sdk 2.9.0"
key_files:
  created:
    - "src/Onboarding.Application/Common/IKeycloakUserService.cs"
    - "src/Onboarding.Infrastructure/Persistence/AppDbContext.cs"
    - "src/Onboarding.Infrastructure/Persistence/Configurations/ClientConfiguration.cs"
    - "src/Onboarding.Infrastructure/Repositories/ClientRepository.cs"
    - "src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs"
    - "src/Onboarding.Infrastructure/DependencyInjection.cs"
  modified:
    - "src/Onboarding.Domain/Repositories/IClientRepository.cs (adicionado DeleteAsync)"
    - "src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj (5 NuGet packages)"
decisions:
  - "IKeycloakUserClient (não IKeycloakClient) — o SDK 2.9.0 expõe interfaces segregadas por domínio; IKeycloakUserClient é o correto para operações de usuário"
  - "ClientId.Parse() e ClientSecret.Parse() necessários no Duende 4.2.0 — string não converte implicitamente para esses tipos fortemente tipados"
  - "Exact=true em GetUsersRequestParameters.Email — evita matches parciais na busca do usuário recém-criado no Keycloak"
  - "FindAsync + Remove em DeleteAsync (não ExecuteDeleteAsync) — respeita change tracking do EF Core"
  - "IDistributedCache comentado em AddInfrastructure — delegado para Program.cs (também necessário para IdempotencyFilter do plano 05-04)"
metrics:
  duration_minutes: 35
  completed_date: "2026-04-06"
  tasks_completed: 2
  files_created: 6
  files_modified: 2
---

# Phase 05 Plan 02: Infrastructure Layer Summary

**One-liner:** EF Core AppDbContext + ClientRepository (com compensation DeleteAsync) + KeycloakUserService via IKeycloakUserClient SDK + AddInfrastructure() DI extension, todos compilando com 0 erros.

## What Was Built

Implementada a camada de Infrastructure completa para suportar o fluxo de registro de clientes PF/PJ:

1. **IClientRepository.DeleteAsync** adicionado — contrato de compensação para REG-06 (rollback se Keycloak falhar após app_db persist)
2. **IKeycloakUserService** criado na camada Application — abstração que isola o SDK do Keycloak da lógica de aplicação, permitindo unit tests com NSubstitute
3. **AppDbContext** — DbContext com `DbSet<Client>` aplicando configuração separada via `IEntityTypeConfiguration<Client>`
4. **ClientConfiguration** — Mapeamento completo: colunas snake_case, `HasConversion` para todos os value objects (Email, PhoneNumber, Cpf, Cnpj), índices únicos parciais via `HasFilter` para CPF e CNPJ nullable (REG-05)
5. **ClientRepository** — Implementação EF Core de todos os métodos com normalização de input (remove pontuação antes de comparar CPF/CNPJ)
6. **KeycloakUserService** — Cria usuário no Keycloak via Admin API e busca o UUID gerado pelo email; deleta por email como no-op se não existir
7. **AddInfrastructure()** — Extension method registrando DbContext, ClientRepository, CC token management (Duende), Keycloak Admin HTTP client, KeycloakUserService

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Adicionar DeleteAsync ao IClientRepository e criar IKeycloakUserService | 02a4199 | IClientRepository.cs, IKeycloakUserService.cs |
| 2 | NuGet packages + AppDbContext + ClientRepository + KeycloakUserService + AddInfrastructure | 2085bba | 5 novos arquivos Infrastructure, csproj |

## Verification Results

- `dotnet build Onboarding.slnx` — Compilação com êxito, 0 Erro(s)
- `dotnet test tests/Onboarding.Domain.Tests/` — 39 aprovados, 4 falhas pré-existentes (stubs TDD RED do plano 05-01)
- Todos os 13 acceptance criteria do plano verificados e confirmados

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Tipo correto IKeycloakUserClient em vez de IKeycloakClient**
- **Found during:** Task 2 — inspeção do XML de documentação do SDK
- **Issue:** O plano mencionava `IKeycloakClient` como tipo a injetar, mas o SDK 2.9.0 expõe `IKeycloakUserClient` como interface segregada para operações de usuário
- **Fix:** `KeycloakUserService` injeta `IKeycloakUserClient` (namespace `Keycloak.AuthServices.Sdk.Admin`)
- **Files modified:** `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs`
- **Commit:** 2085bba

**2. [Rule 1 - Bug] ClientId.Parse() e ClientSecret.Parse() para Duende 4.2.0**
- **Found during:** Task 2 — erro de compilação CS0029
- **Issue:** Duende 4.2.0 usa tipos strongly-typed `ClientId` e `ClientSecret` que não aceitam conversão implícita de `string`
- **Fix:** Usado `ClientId.Parse(adminClientId)` e `ClientSecret.Parse(adminClientSecret)` em `DependencyInjection.cs`
- **Files modified:** `src/Onboarding.Infrastructure/DependencyInjection.cs`
- **Commit:** 2085bba

## Known Stubs

Nenhum stub neste plano — todas as implementações são concretas e funcionais. Os 4 testes falhando são stubs TDD RED do plano 05-01 (intencionais, aguardando Green no plano 05-03).

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: config-exposure | DependencyInjection.cs | Keycloak:AdminClientSecret lido de IConfiguration — fail-fast (InvalidOperationException) se ausente, mas requer que o chamador (Program.cs) configure via env vars / Docker secrets, nunca appsettings.json |

## Self-Check: PASSED

- [x] `src/Onboarding.Application/Common/IKeycloakUserService.cs` — existe
- [x] `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` — existe
- [x] `src/Onboarding.Infrastructure/Persistence/Configurations/ClientConfiguration.cs` — existe
- [x] `src/Onboarding.Infrastructure/Repositories/ClientRepository.cs` — existe
- [x] `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — existe
- [x] `src/Onboarding.Infrastructure/DependencyInjection.cs` — existe
- [x] Commit 02a4199 — existe (Task 1)
- [x] Commit 2085bba — existe (Task 2)
