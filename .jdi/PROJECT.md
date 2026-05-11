# Onboarding PF/PJ — Keycloak

## Vision
Sistema de onboarding seguro para clientes PJ com gestão de funcionários, permissões via Keycloak e (milestone v8.0) gestão cadastral de fundos de investimento. Isolamento multi-tenant entre empresas é requisito de primeira classe.

## Type
Full-stack web app (API + 2 SPAs)

## Status
**Adopted** em 2026-05-11. Codigo pre-existente — JDI adicionado depois. Repo ja possui sistema paralelo `.planning/` (legado, milestones v1-v8). JDI segue daqui pra frente, sem conflito.

## Stack (detected + confirmed)
- **Language:** C# / TypeScript
- **Backend:** .NET 10 (ASP.NET Core Controllers + EF Core 10) + PostgreSQL 16
- **Frontend client:** React 19 + Vinxi (Vite) + TanStack Router + Tailwind 4 + react-hook-form + zod
- **Frontend backoffice:** same stack as client, projeto separado (constraint arquitetural — sem code-sharing)
- **Auth:** Keycloak 26.1 (hardened). Client = ACF+PKCE (Phase 33). Backoffice = ACF+PKCE com tema customizado (D-feedback).
- **Test backend:** xUnit + Shouldly + NSubstitute + coverlet (cobertura 80%)
- **Test frontend:** Vitest + Playwright (e2e)
- **Lint/format:** ESLint 9 (frontend), .editorconfig + analyzers (.NET)
- **Hooks:** husky + lint-staged
- **Observability:** Serilog + OpenTelemetry
- **Security pipeline (13 checks):** Semgrep, CodeQL, Trivy, Dependabot, Syft, ZAP, Dockle, Checkov, Kubescape, Gitleaks, TruffleHog
- **Conventional commits:** sim (~30/30 commits recentes seguem)

## Code Design
**LOCKED:** Clean Architecture + DDD + CQRS manual

Confirmado pela deteccao automatica:
- `src/Onboarding.Domain/` (Aggregates, ValueObjects, Repositories interfaces, Exceptions, Common)
- `src/Onboarding.Application/` (Commands, Queries, DTOs por slice: Companies, Fundos, Admin, Auth, Common)
- `src/Onboarding.Infrastructure/` (Persistence, Repositories, Keycloak, Services)
- `src/Onboarding.API/` (Controllers, Middleware, Security, Filters, Observability)

CQRS via DI manual (sem MediatR — restricao OSS-only documentada em CLAUDE.md). FluentValidation para validators. Shouldly em vez de FluentAssertions.

## Slug
onboarding-keycloak

## Existing assets (snapshot em 2026-05-11)

Modulos/diretorios encontrados (agrupados):
- `src/Onboarding.Domain/Aggregates/` — Company, Employee, Audit, AdminAuditLog, PasswordReset, Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo (10 aggregates)
- `src/Onboarding.Domain/ValueObjects/`, `Common/`, `Exceptions/`, `Repositories/`
- `src/Onboarding.Application/Companies/` — Commands (24 files: register, employee CRUD, access groups, password reset), Queries, DTOs
- `src/Onboarding.Application/Fundos/` — Commands (TipoAtivo, Cedente PF/PJ, ConsultoriaFundo, Custodiante, Fundo lifecycle), Queries, DTOs
- `src/Onboarding.Application/Admin/`, `Auth/`, `Common/`, `Services/`
- `src/Onboarding.API/Controllers/` — incluindo `FundosController.cs` (em desenvolvimento — phase 48)
- `src/Onboarding.API/Security/PermissionPolicyConstants.cs`
- `src/Onboarding.API/Middleware/`, `Filters/`, `Configuration/`, `Observability/`
- `src/Onboarding.Infrastructure/Persistence/`, `Repositories/`, `Keycloak/`, `Services/`
- `tests/Onboarding.Domain.Tests/`, `Application.Tests/`, `API.Tests/`, `Integration.Tests/`
- `tests/keycloak-hardening/`, `tests/uat-tests.http`, `tests/run-uat.mjs`
- `frontend/client/src/` — SPA cliente PJ registration + profile
- `frontend/backoffice/src/` — SPA admin (employee/access-group management, audit log)
- `keycloak/` — realm exports, themes (PKCE custom)
- `infra/`, `scripts/`, `docs/`, `.github/`, `.semgrep/`

Total: 267 .cs em src/, 124 .cs em tests/, 168 .ts/.tsx em frontend.

Schema/migrations: EF Core migrations em `src/Onboarding.Infrastructure/Persistence/Migrations/` (inclui AddFundosModule recente)
Routes/endpoints: Controllers em `src/Onboarding.API/Controllers/` — registration, auth, admin, employees, access-groups, audit-log, fundos (WIP)
Existing tests: xUnit 4 projetos, cobertura 80% enforced via coverlet+CI

**Importante:** Estes assets sao contexto pro planner, NAO TODO. Phases adicionam novas features.

## Global constraints
- Cobertura minima 80% (apenas em codigo NOVO criado apos D-2 boundary)
- Conventional commits (ja em uso)
- Atomic commits — 1 task = 1 commit
- Codigo, commits, PRs: ingles
- Discussao, docs em `.jdi/`: pt-BR
- i18n: nunca string hardcoded em pt-BR no JSX
- **OSS-only:** todos NuGet packages devem ser MIT/Apache 2.0 — sem MediatR (commercial), sem FluentAssertions (paid). Usar CQRS manual via DI, Shouldly
- **Isolamento multi-tenant:** CRITICO — qualquer leak entre empresas eh vulnerabilidade. Aggregates company-scoped tem HasQueryFilter + ClientId
- **Frontend separation:** `frontend/client` e `frontend/backoffice` totalmente independentes — sem shared code, sem cross-imports
- Prioridade quando conflita: Seguranca > Performance > Boas praticas

## Research notes
- Repo possui sistema paralelo `.planning/` (legado). JDI nao toca nele — segue adiante.
- Phase 48 (.planning) atualmente em planning: API + Permissions for Fundos module — vira phase 1 do JDI roadmap.
- Backoffice migrou de ROPC pra ACF+PKCE em phase 33. ROPC legado pode ser removido futuro.

## LLM config
- Provider: Anthropic Claude (default — projeto roda em Claude Code)
- Modelo padrao: Opus pro planejamento/research, Sonnet pra execucao
