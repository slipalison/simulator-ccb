# Phase 48: API + Permissions for Fundos module — Context

## Goal
Expor endpoints REST para o modulo Fundos (ConsultoriaFundo, Custodiante, TipoAtivo, Fundo, Cedente PF/PJ) via `FundosController` + criar `AdminFundosController` para listagem cross-company read-only + registrar policies `funds:read/write/delete/manage` + estender access groups default (`admin-empresa` recebe `funds:manage`, `viewer` recebe `funds:read`) — com isolamento multi-tenant aplicado em todas mutacoes e queries company-scoped.

## Locked decisions

- **D-7 (2026-05-11):** Plan 48-01 pre-existente (FundosController.cs + 3 files modificados) foi DESCARTADO em commit `0e73aee`. JDI Phase 48 re-implementa do zero atraves do workflow standard (plan -> do -> verify -> ship). Motivo: comecar do estado limpo sob governanca JDI ao inves de absorver trabalho pre-existente nao verificado.

- **D-8 (2026-05-11):** AdminFundosController escopo MINIMO — apenas List cross-company para 4 entidades: GET `/admin/fundos`, `/admin/consultorias`, `/admin/custodiantes`, `/admin/cedentes`. Cada endpoint paginado, retorna entity data + nome da empresa (join via ClientId). SEM detail-by-id, SEM audit drill-down, SEM admin overrides (status force, restore, etc). Autenticacao = `BearerBackoffice` + `Policy = PermissionPolicies.CrossCompanyAccess`. Implementacao via query handlers dedicados (`Admin*QueryHandler`) que usam `IgnoreQueryFilters()` + recebem `CompanyId` quando filtragem opcional — pattern D-12 EmployeeRepository.

- **D-9 (2026-05-11):** Endpoint `POST /api/fundos/{id}/status` (transicao state machine) usa body MINIMO `{ NewStatus }`. ActorSub/ActorEmail extraidos do JWT. NAO inclui campo `Motivo` nem `EvidenciaUrl` no MVP. AdminAuditLog registra transition automaticamente (entity_id, from_status, to_status, actor, timestamp). Evolucao futura (motivo/evidencia) pode ser adicionada sem breaking change.

- **D-10 (2026-05-11):** Cedente uniqueness eh COMPANY-SCOPED, nao global. Composite unique indexes `(ClientId, Cpf)` para Cedente PF e `(ClientId, Cnpj)` para Cedente PJ. Mesma empresa nao pode ter 2 cedentes mesmo CPF/CNPJ; empresas diferentes podem ter cedente com mesmo documento independentemente. Mantem isolamento multi-tenant (D-5) — codigo CR-01 pattern de Phase 46. Garante zero leak entre tenants mesmo via erro 409.

## Canonical refs

- `.planning/phases/48-api-permissions/48-01-PLAN.md` (GSD plan — referencia, NAO blueprint vinculante apos discard)
- `.planning/phases/48-api-permissions/48-02-PLAN.md` (GSD plan — idem)
- `src/Onboarding.API/Controllers/CompaniesController.cs` (pattern reference — BearerClient + ICommandHandler injection + try/catch local pra `DuplicateEntityException` -> 409 + `KeyNotFoundException` -> 404)
- `src/Onboarding.API/Controllers/AdminUserController.cs` (pattern reference — BearerBackoffice + CrossCompanyAccess + repository IgnoreQueryFilters)
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs` (constants `Permissions.FundsRead/Write/Delete/Manage` ja existem desde Phase 45)
- `src/Onboarding.Application/Fundos/` (Commands + Queries + DTOs + Validators ja prontos do Phase 47 — handler signatures em 48-01-PLAN.md secao `<interfaces>`)
- `src/Onboarding.API/Middleware/GlobalExceptionHandler.cs` (estado pre-48-01 em commit `968eefb` — deve receber mapeamento `DuplicateEntityException -> 409` + `InvalidStateTransitionException -> 400` durante /jdi-do 48)

## Convencao adotada

- **Validation errors:** 422 UnprocessableEntity via `ToValidationProblem(FluentValidation.Results.ValidationResult)` helper (matches CompaniesController/AdminUserController existing pattern).
- **Authentication scheme:** `BearerClient` para FundosController (PJ user), `BearerBackoffice` para AdminFundosController (admin role).
- **Route prefix:** `/api/fundos/{entity}` para PJ endpoints; `/api/admin/fundos/{entity}` (ou similar) para admin. Confirmar prefix admin durante /jdi-plan 48 alinhado com AdminUserController.
- **DTO contracts:** ja definidos em Phase 47 — Controllers consomem direto sem novos DTOs.
- **Permission constants:** `PermissionPolicyConstants.FundRead/FundWrite/FundDelete/FundManage` (string keys) + `Permissions.FundsRead/Write/Delete/Manage` (claim values). 4 policies registrar em `Program.cs` AddAuthorization.

## Out of scope (capturado em todos.md)

- FundoCedente relationship CRUD -> Phase 49
- N-N relationships (Cedente<->TipoAtivo, Fundo<->TipoAtivo) -> Phase 49
- Frontend Fundos UI -> Phases 50/51
- Integration tests Testcontainers -> Phase 52
- Vinxi -> Vinext migration -> Phase 53
- Motivo/EvidenciaUrl em status transition -> backlog (revisitar apos feedback usuario PJ)
- Admin status force override -> backlog (requer D-decision separada, auditoria reforcada)
- Idempotency-Key header pra retry-safe POSTs -> backlog

## Notes

- Phase 47 jah cobriu Application layer 3/3 plans (SUMMARYs presentes, sem VERIFICATION formal). Reviewer JDI no /jdi-verify pode incluir verify retroativo do Phase 47 se relevante a integridade do 48.
- Coverage 80% aplica APENAS em files novos criados apos boundary `968eefb` (D-2). FundosController re-criado entra em escopo coverage.
- Plan JDI deve criar 2 waves: Wave 1 = 48-01 reimplementacao (3 simple entities) + Wave 2 = 48-02 (Fundo/Cedente + AdminFundosController + access groups seed). Dependencia: Wave 2 depende de Wave 1 (FundosController criado).
- Reviewer playwright mandatory (G7 backend) — UAT regression suite roda contra novos endpoints (POST/GET/PUT consultorias/custodiantes/tipos-ativo/fundos/cedentes + status transition + admin list).
- Security reviewer (G1) audita HasQueryFilter em novos aggregates touched + AuthZ policy coverage em todos novos endpoints + ActorSub/ActorEmail captured em todas mutacoes.
