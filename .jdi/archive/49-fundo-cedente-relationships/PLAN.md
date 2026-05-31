# Phase 50: fundo-cedente-relationships — Plan  (slug: fundo-cedente-relationships)

## Goal

Modelar e expor as três associações N-N do módulo Fundos como aggregates de relacionamento simétricos (D-21) com payload completo (limites + janela de datas + Status enum), enforce REL-09 com defesa em profundidade, e manter o pattern de state-machine action herdado de Phase 48 (D-9/D-22).

## Locked decisions (Phase 50)

- **D-18:** REL-09 enforced via Postgres partial unique index (`WHERE Status='ATIVO'`) + invariante de domínio no aggregate `FundoCedente`.
- **D-19:** Status é coluna explícita enum `ATIVO`/`INATIVO`/`HISTORICO`, não derivada da janela de datas.
- **D-20:** Janela `[data_inicio, data_fim)` half-open com `data_fim` nullable (vigência infinita = NULL); `data_inicio` obrigatório.
- **D-21:** Três aggregates de associação simétricos com mesmo shape (`FundoCedente`, `CedenteTipoAtivo`, `FundoTipoAtivo`).
- **D-22:** Mudança de Status via `POST /api/.../status` com body `{ NewStatus }` + AdminAuditLog automático (pattern Phase 48 D-9).

## Tasks

### Wave 1

#### T-1: Three relationship aggregates (Domain layer)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Domain/Aggregates/FundoCedenteAggregate/FundoCedente.cs` (new)
  - `src/Onboarding.Domain/Aggregates/FundoCedenteAggregate/RelationshipStatus.cs` (new, shared enum — actual file location may be `Common/`; doer decides)
  - `src/Onboarding.Domain/Aggregates/CedenteTipoAtivoAggregate/CedenteTipoAtivo.cs` (new)
  - `src/Onboarding.Domain/Aggregates/FundoTipoAtivoAggregate/FundoTipoAtivo.cs` (new)
  - `src/Onboarding.Domain/ValueObjects/LimiteExposicao.cs` (new, value object encapsulando percentual + valor)
  - `src/Onboarding.Domain/ValueObjects/JanelaVigencia.cs` (new, half-open date window)
  - `src/Onboarding.Domain/Exceptions/DuplicateActiveAssociationException.cs` (new)
  - `src/Onboarding.Domain/Exceptions/InvalidStatusTransitionException.cs` (new)
  - `src/Onboarding.Domain/Repositories/IFundoCedenteRepository.cs` (new) — interfaces
  - `src/Onboarding.Domain/Repositories/ICedenteTipoAtivoRepository.cs` (new)
  - `src/Onboarding.Domain/Repositories/IFundoTipoAtivoRepository.cs` (new)
  - `tests/Onboarding.Domain.Tests/FundoCedenteTests.cs` (new) — aggregate invariants
  - `tests/Onboarding.Domain.Tests/CedenteTipoAtivoTests.cs` (new)
  - `tests/Onboarding.Domain.Tests/FundoTipoAtivoTests.cs` (new)
- **Acceptance:**
  - Cada aggregate carrega `Id` (Guid), refs aos pais (`FundoId`/`CedenteId`/`TipoAtivoId`), `Status` (RelationshipStatus enum), `Janela` (JanelaVigencia VO), `Limite` (LimiteExposicao VO).
  - `LimiteExposicao` aceita `Percentual` (decimal 0..100) e `Valor` (decimal > 0) ambos opcionais; planner adota regra "pelo menos um obrigatório" como default (reviewer aprova ou ajusta — out-of-scope decision).
  - `JanelaVigencia` valida `data_inicio` obrigatório, `data_fim` nullable, `data_fim > data_inicio` quando setado.
  - State machine no domínio: `ATIVO ↔ INATIVO`, `* → HISTORICO`, `HISTORICO` terminal (não volta a ATIVO). Lança `InvalidStatusTransitionException` em transições inválidas.
  - `FundoCedente` tem método `ActivateGuard()` que verifica REL-09 em-memória antes de persistir (defesa-em-profundidade junto com DB index — D-18).
  - Cobertura ≥80% nos arquivos novos (D-2).
- **Dependencies:** none
- **Test:** `dotnet test tests/Onboarding.Domain.Tests` (xUnit + Shouldly + NSubstitute para repository mocks).
- **Status:** pending

### Wave 2

#### T-2: EF Core configurations + migration (Infrastructure layer)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp + cross-cutting security review (auto-trigger por `**/Permission*`/`**/Auth*` não aplica; trigger via keyword "migration" + multi-tenant filter coverage)
- **Files modified:**
  - `src/Onboarding.Infrastructure/Persistence/Configurations/FundoCedenteConfiguration.cs` (new)
  - `src/Onboarding.Infrastructure/Persistence/Configurations/CedenteTipoAtivoConfiguration.cs` (new)
  - `src/Onboarding.Infrastructure/Persistence/Configurations/FundoTipoAtivoConfiguration.cs` (new)
  - `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` (DbSet additions + OnModelCreating apply config)
  - `src/Onboarding.Infrastructure/Repositories/FundoCedenteRepository.cs` (new) — 3 repository impls
  - `src/Onboarding.Infrastructure/Repositories/CedenteTipoAtivoRepository.cs` (new)
  - `src/Onboarding.Infrastructure/Repositories/FundoTipoAtivoRepository.cs` (new)
  - `src/Onboarding.Infrastructure/Persistence/Migrations/<timestamp>_AddRelationshipAggregates.cs` (new) + `.Designer.cs` + snapshot update
- **Acceptance:**
  - Migration cria 3 tabelas com FKs apropriadas (cascading rules: FundoCedente → Fundo/Cedente RESTRICT; análogo nas outras).
  - Partial unique index em `FundoCedente`: `CREATE UNIQUE INDEX ix_fundo_cedente_active ON "FundoCedente" ("FundoId", "CedenteId") WHERE "Status" = 'ATIVO'` (D-18).
  - Decisão por aggregate: Cedente↔TipoAtivo e Fundo↔TipoAtivo levam partial unique index análogo? Doer propõe `(CedenteId, TipoAtivoId) WHERE Status='ATIVO'` e `(FundoId, TipoAtivoId) WHERE Status='ATIVO'` para evitar duplicata-ativa também (não bloqueia REL-09 que é só Fundo-Cedente, mas mantém uniformidade D-21).
  - `data_fim` column nullable timestamptz; `data_inicio` NOT NULL.
  - Status column é text constrained via CHECK ou enum mapping (.NET Enum → string via EF Conversion).
  - HasQueryFilter NÃO aplicado nos aggregates de relacionamento — o tenant scoping já é feito via parent aggregate (Fundo.ClienteId, Cedente.ClienteId). Reviewer security audita.
  - Migration roundtrip: `dotnet ef database update` aplica + `dotnet ef migrations remove` reverte clean.
- **Dependencies:** T-1
- **Test:** `dotnet test tests/Onboarding.Application.Tests` (handler tests via NSubstitute on repository) confirma compilação contra novo schema; migration assertion via Integration.Tests em T-7.
- **Status:** pending

### Wave 3 (parallel-eligible)

#### T-3: Application layer — FundoCedente commands/queries/handlers
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Application/Fundos/Commands/CreateFundoCedente/CreateFundoCedenteCommand.cs` (new)
  - `src/Onboarding.Application/Fundos/Commands/CreateFundoCedente/CreateFundoCedenteHandler.cs` (new)
  - `src/Onboarding.Application/Fundos/Commands/CreateFundoCedente/CreateFundoCedenteValidator.cs` (new, FluentValidation)
  - `src/Onboarding.Application/Fundos/Commands/UpdateFundoCedenteLimite/...` (3 files — Command + Handler + Validator)
  - `src/Onboarding.Application/Fundos/Commands/TransitionFundoCedenteStatus/...` (3 files — state-machine action D-22)
  - `src/Onboarding.Application/Fundos/Queries/GetFundoCedentes/...` (paginated list por FundoId)
  - `src/Onboarding.Application/Fundos/DTOs/FundoCedenteDto.cs` (new) — read DTO
  - `tests/Onboarding.Application.Tests/Fundos/FundoCedenteHandlerTests.cs` (new) — handler tests com NSubstitute
- **Acceptance:**
  - CommandHandler implementa `ICommandHandler<TCommand>` (sem MediatR — D-3).
  - Create handler valida REL-09 em-memória ANTES do save (defesa-em-profundidade junto com DB).
  - StatusTransition handler escreve AdminAuditLog automaticamente (reusar serviço de Phase 48 D-9).
  - Cobertura ≥80% nos arquivos novos.
- **Dependencies:** T-2
- **Test:** `dotnet test tests/Onboarding.Application.Tests --filter "FullyQualifiedName~FundoCedente"`.
- **Status:** pending

#### T-4: Application layer — CedenteTipoAtivo + FundoTipoAtivo commands/queries/handlers
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Application/Fundos/Commands/CreateCedenteTipoAtivo/...` (3 files)
  - `src/Onboarding.Application/Fundos/Commands/UpdateCedenteTipoAtivoLimite/...` (3 files)
  - `src/Onboarding.Application/Fundos/Commands/TransitionCedenteTipoAtivoStatus/...` (3 files)
  - `src/Onboarding.Application/Fundos/Commands/CreateFundoTipoAtivo/...` (3 files)
  - `src/Onboarding.Application/Fundos/Commands/UpdateFundoTipoAtivoLimite/...` (3 files)
  - `src/Onboarding.Application/Fundos/Commands/TransitionFundoTipoAtivoStatus/...` (3 files)
  - `src/Onboarding.Application/Fundos/Queries/GetCedenteTiposAtivos/...`
  - `src/Onboarding.Application/Fundos/Queries/GetFundoTiposAtivos/...`
  - `src/Onboarding.Application/Fundos/DTOs/CedenteTipoAtivoDto.cs`, `FundoTipoAtivoDto.cs` (new)
  - `tests/Onboarding.Application.Tests/Fundos/CedenteTipoAtivoHandlerTests.cs` (new)
  - `tests/Onboarding.Application.Tests/Fundos/FundoTipoAtivoHandlerTests.cs` (new)
- **Acceptance:**
  - Mesma estrutura simétrica de T-3 (D-21) — reusa pattern de CommandHandler manual + FluentValidation.
  - StatusTransition handlers escrevem AdminAuditLog.
  - Cobertura ≥80% nos arquivos novos.
- **Dependencies:** T-2 (disjoint de T-3 em files_modified — diferentes pastas Command/Query)
- **Test:** `dotnet test tests/Onboarding.Application.Tests --filter "FullyQualifiedName~TipoAtivo"`.
- **Status:** pending

### Wave 4

#### T-5: API controllers (3 new)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.API/Controllers/FundoCedentesController.cs` (new) — `/api/fundos/{fundoId}/cedentes/*`
  - `src/Onboarding.API/Controllers/FundoTiposAtivosController.cs` (new) — `/api/fundos/{fundoId}/tipos-ativos/*`
  - `src/Onboarding.API/Controllers/CedenteTiposAtivosController.cs` (new) — `/api/cedentes/{cedenteId}/tipos-ativos/*`
  - `src/Onboarding.API/Configuration/DependencyInjection.cs` (or `Program.cs`) — DI registrations
  - `tests/Onboarding.API.Tests/FundoCedentesControllerTests.cs` (new)
  - `tests/Onboarding.API.Tests/FundoTiposAtivosControllerTests.cs` (new)
  - `tests/Onboarding.API.Tests/CedenteTiposAtivosControllerTests.cs` (new)
- **Acceptance:**
  - 3 controllers, cada um com endpoints: POST (create), PATCH limites, POST `.../status` (D-22 state-machine), GET list paginated, GET by-id.
  - Cross-tenant guard inline (pattern de Phase 48 iter 2 commit `eb5bc24`): controller verifica `entity is null || entity.ClienteId != _currentCompanyService.CompanyId` → 404 (não 403 — evita leak de existência cross-tenant).
  - Policies via `[Authorize(Policy = PermissionPolicies.FundsWrite)]` etc — reusa políticas Phase 48.
  - 422 com `application/problem+json` para FluentValidation errors.
  - 409 para `DuplicateActiveAssociationException` (REL-09).
  - 400 com `from/to` detail para `InvalidStatusTransitionException`.
  - Cobertura ≥80% nos arquivos novos.
- **Dependencies:** T-3, T-4
- **Test:** `dotnet test tests/Onboarding.API.Tests`.
- **Status:** pending

### Wave 5 (parallel-eligible)

#### T-6: AdminFundosController extensions (cross-company read-only)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.API/Controllers/AdminFundosController.cs` — adicionar 3 endpoints read-only: `GET /api/admin/fundo-cedentes`, `GET /api/admin/fundo-tipos-ativos`, `GET /api/admin/cedente-tipos-ativos`
  - `src/Onboarding.Infrastructure/Queries/AdminFundoCedentesQueryHandler.cs` (new) — usa `IgnoreQueryFilters()` análogo a Phase 48
  - `src/Onboarding.Infrastructure/Queries/AdminFundoTiposAtivosQueryHandler.cs` (new)
  - `src/Onboarding.Infrastructure/Queries/AdminCedenteTiposAtivosQueryHandler.cs` (new)
  - `src/Onboarding.Application/Admin/Queries/...` (3 query DTOs/contracts)
  - `tests/Onboarding.API.Tests/AdminFundosControllerRelationshipsTests.cs` (new)
- **Acceptance:**
  - Class-level `[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = "CrossCompanyAccess")]`.
  - `IgnoreQueryFilters()` nos query handlers + JOIN com Companies para projetar nome da empresa.
  - GET-only — sem mutation cross-company (D-8 phase 48 precedent).
  - Cobertura ≥80% nos arquivos novos.
- **Dependencies:** T-5
- **Test:** `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~AdminFundosControllerRelationships"`.
- **Status:** pending

#### T-7: Integration tests (Testcontainers PostgreSQL real)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `tests/Onboarding.Integration.Tests/Fundos/FundoCedenteIntegrationTests.cs` (new)
  - `tests/Onboarding.Integration.Tests/Fundos/CedenteTipoAtivoIntegrationTests.cs` (new)
  - `tests/Onboarding.Integration.Tests/Fundos/FundoTipoAtivoIntegrationTests.cs` (new)
- **Acceptance:**
  - Mínimo 18 cenários (6 por aggregate de relacionamento, simétrico):
    1. POST 201 happy path.
    2. POST duplicate ativa → 409 (REL-09 enforced — particularmente importante em FundoCedente).
    3. Cross-tenant POST de outro CompanyId → 404 (cross-tenant guard).
    4. PATCH limite happy path.
    5. POST `.../status` ATIVO → INATIVO → 200; INATIVO → ATIVO → 200; HISTORICO terminal → 400.
    6. GET list paginated retorna apenas da própria company.
  - Cenário extra para FundoCedente: race condition simulada (2 POSTs concorrentes do mesmo par com Status=ATIVO) — DB partial index rejeita o segundo com `DbUpdateException` traduzida pelo `GlobalExceptionHandler` em 409.
  - Cenário admin: `BearerBackoffice` + `CrossCompanyAccess` vê linhas de Company A + Company B na mesma response (T-6).
  - Testcontainers PostgreSQL real; sem mocks de DB.
- **Dependencies:** T-5, T-6
- **Test:** `dotnet test tests/Onboarding.Integration.Tests --filter "FullyQualifiedName~Fundo|FullyQualifiedName~Cedente"`.
- **Status:** pending

## Execution
- Total tasks: 7
- Waves: 5 (W1: T-1; W2: T-2; W3: T-3 + T-4 parallel; W4: T-5; W5: T-6 + T-7 parallel)
- Estimated parallel speedup: ~7/5 ≈ 1.4x (relationship aggregates são tightly coupled — sequencing dominante)

## Files modified (all tasks)
- Domain layer (T-1): 14 new files (3 aggregates + 2 value objects + 2 exceptions + 3 repository interfaces + 3 domain tests + 1 shared enum)
- Infrastructure (T-2): 7 new files + 1 migration + AppDbContext edit + snapshot update
- Application (T-3 + T-4): ~30 new files (3 commands × 3 aggregates × 3 files each + 3 queries + 3 DTOs + 3 handler test files)
- API (T-5): 3 new controllers + DI wiring + 3 API test files
- Admin (T-6): 1 controller edit + 3 admin query handlers + 1 admin test file
- Integration (T-7): 3 integration test files

## Test requirements
- Domain: `dotnet test tests/Onboarding.Domain.Tests`
- Application: `dotnet test tests/Onboarding.Application.Tests`
- API: `dotnet test tests/Onboarding.API.Tests`
- Integration: `dotnet test tests/Onboarding.Integration.Tests` (Testcontainers required — Docker daemon must be up)
- Coverage gate: 80% on new files post-`968eefb` (D-2)
- Cross-cutting security (auto-trigger via "migration" keyword + multi-tenant filter coverage): full pipeline at `/jdi-verify`
- Mandatory Playwright regression on API endpoints (G7 per backend reviewer agent): smoke against any new endpoint reachable from the SPA — likely deferred since this phase is API-only (frontend UI is phases 51/52)
