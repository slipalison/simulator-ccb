---
phase: 37-domain-model-redesign
plan: 03
subsystem: database, infra
tags: [ef-core, configuration, repositories, migration, hasqueryfilter, company-isolation, company, employee, access-group]

# Dependency graph
requires:
  - phase: 37-domain-model-redesign
    plan: 01
    provides: Company, Employee, AccessGroup, Permissions, TermsAcceptance domain types + ICompanyRepository/IEmployeeRepository/IAccessGroupRepository interfaces
  - phase: 37-domain-model-redesign
    plan: 02
    provides: 139 domain unit tests passing, zero Client references
provides:
  - EF Core fluent configs for Company, Employee, AccessGroup with VO conversions
  - HasQueryFilter on Employee and AccessGroup filtering by CompanyId (D-17)
  - ICurrentCompanyService + CurrentCompanyService for query filter injection
  - AppDbContext with DbSet<Company>, DbSet<Employee>, DbSet<AccessGroup> — no DbSet<Client>
  - EmployeeRepository and AccessGroupRepository implementations
  - EF Core migration: drops 'clients', creates 'companies', 'employees', 'access_groups'
  - Unique index on companies.cnpj (REG-02)
  - Cnpj and Cpf nullable in DB for LGPD Anonymize() support
affects: [38-employee-registration, 39-keycloak-groups, 40-client-frontend, 41-backoffice-employee]

# Tech tracking
tech-stack:
  added: []
  patterns: [hasqueryfilter-company-isolation, icurrentcompanyservice-injection, csv-permissions-conversion, vo-conversion-nullable]

key-files:
  created:
    - src/Onboarding.Application/Common/ICurrentCompanyService.cs
    - src/Onboarding.Infrastructure/Persistence/CurrentCompanyService.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/AccessGroupConfiguration.cs
    - src/Onboarding.Infrastructure/Repositories/EmployeeRepository.cs
    - src/Onboarding.Infrastructure/Repositories/AccessGroupRepository.cs
    - src/Onboarding.Infrastructure/Persistence/Migrations/20260426021430_ReplaceClientWithCompanyEmployee.cs
  modified:
    - src/Onboarding.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/AppDbContext.cs
    - src/Onboarding.Infrastructure/Persistence/AppDbContextFactory.cs
    - src/Onboarding.Infrastructure/DependencyInjection.cs

key-decisions:
  - "Cnpj e Cpf nullable no DB — necessario para Anonymize() LGPD que seta VO para null!"
  - "EmployeeRepository.GetPagedByCompanyAsync usa filtro explicito CompanyId alem do HasQueryFilter — suporta admin bypass"
  - "CompanyConfiguration recebe ICurrentCompanyService no construtor por consistencia, embora nao use HasQueryFilter"

patterns-established:
  - "HasQueryFilter com ICurrentCompanyService injetado via construtor — padrao de isolamento entre empresas (D-17)"
  - "VOs nullable no DB com HasConversion null-safe — suporta Anonymize() LGPD"
  - "Permissions como CSV string via HasConversion — List<string> <-> comma-separated"

requirements-completed: [REG-02, REG-04, REG-05]

# Metrics
duration: 10min
completed: 2026-04-26
---

# Phase 37 Plan 03: Infrastructure Layer Summary

**EF Core configs com HasQueryFilter por CompanyId, repositories, ICurrentCompanyService e migration que dropa clients e cria companies/employees/access_groups**

## Performance

- **Duration:** 10 min
- **Started:** 2026-04-26T02:06:23Z
- **Completed:** 2026-04-26T02:16:30Z
- **Tasks:** 1
- **Files modified:** 13 (7 created, 4 modified, 2 migration files)

## Accomplishments
- ICurrentCompanyService + CurrentCompanyService — injeção scoped para HasQueryFilter por CompanyId (D-17)
- EmployeeConfiguration com HasQueryFilter `e.CompanyId == _currentCompanyService.CompanyId` — isolamento entre empresas (T-37-03-01)
- AccessGroupConfiguration com HasQueryFilter por CompanyId — isolamento entre empresas (T-37-03-02)
- CompanyConfiguration com unique index em Cnpj com `HasFilter("cnpj IS NOT NULL")` (REG-02)
- AppDbContext: DbSet<Company>, DbSet<Employee>, DbSet<AccessGroup> — zero referências a Client
- EmployeeRepository e AccessGroupRepository implementados com padrão existente
- Migration: drop `clients`, cria `companies`/`employees`/`access_groups` — preserva `admin_audit_logs` e `password_reset_tokens` (D-16)
- Cnpj e Cpf nullable no DB — suporte a Anonymize() LGPD
- 139 testes domain passando, build limpo 0 erros 0 warnings

## Task Commits

1. **Task 1: EF Core configs + AppDbContext + CurrentCompanyService + repositories + migration** - `b60a229` (feat)

## Files Created/Modified
- `src/Onboarding.Application/Common/ICurrentCompanyService.cs` - Interface para injeção de CompanyId no HasQueryFilter
- `src/Onboarding.Infrastructure/Persistence/CurrentCompanyService.cs` - Implementação scoped com CompanyId setável por request
- `src/Onboarding.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs` - Config EF Core com HasQueryFilter, VO conversions, FKs, indexes
- `src/Onboarding.Infrastructure/Persistence/Configurations/AccessGroupConfiguration.cs` - Config EF Core com HasQueryFilter, Permissions CSV, composite unique index
- `src/Onboarding.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs` - Atualizado com ICurrentCompanyService, Cnpj nullable
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` - DbSet<Employee>, DbSet<AccessGroup>, construtor com ICurrentCompanyService
- `src/Onboarding.Infrastructure/Persistence/AppDbContextFactory.cs` - Atualizado para passar CurrentCompanyService no design-time
- `src/Onboarding.Infrastructure/Repositories/EmployeeRepository.cs` - CRUD + GetPagedByCompanyAsync com search/status filters
- `src/Onboarding.Infrastructure/Repositories/AccessGroupRepository.cs` - CRUD + AddRangeAsync, GetByCompanyIdAsync, GetByCompanyAndNameAsync
- `src/Onboarding.Infrastructure/DependencyInjection.cs` - Registra ICurrentCompanyService, IEmployeeRepository, IAccessGroupRepository
- `src/Onboarding.Infrastructure/Persistence/Migrations/20260426021430_ReplaceClientWithCompanyEmployee.cs` - Migration: drop clients, create companies/employees/access_groups
- `src/Onboarding.Infrastructure/Persistence/Migrations/20260426021430_ReplaceClientWithCompanyEmployee.Designer.cs` - Migration designer

## Decisions Made
- Cnpj e Cpf nullable no DB — `Anonymize()` seta VO para `null!` no domain, requer coluna nullable no PostgreSQL
- CompanyConfiguration recebe ICurrentCompanyService por consistência arquitetural com EmployeeConfiguration/AccessGroupConfiguration
- EmployeeRepository.GetPagedByCompanyAsync usa `Where(e => e.CompanyId == companyId)` explícito além do HasQueryFilter — suporta admin endpoints que bypassam o filtro
- Permissions armazenadas como CSV string via `string.Join(",", v)` / `s.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList()`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Cnpj e Cpf nao nullable no DB causa falha em Anonymize()**
- **Found during:** Task 1 (verificação da migration gerada)
- **Issue:** Migration gerada com `cnpj` e `cpf` como `nullable: false` — mas Company.Anonymize() e Employee.Anonymize() setam VOs para `null!`, causaria SQL error em runtime
- **Fix:** Adicionado `.IsRequired(false)` nas Property configs de Cnpj e Cpf. Regenerada migration — agora `nullable: true`
- **Files modified:** CompanyConfiguration.cs, EmployeeConfiguration.cs
- **Verification:** Migration gerada com `cnpj` nullable: true, `cpf` nullable: true. Build limpo, 139 testes passando
- **Committed in:** b60a229 (part of task commit)

**2. [Rule 3 - Blocking] AppDbContextFactory sem ICurrentCompanyService**
- **Found during:** Task 1 (build do Infrastructure project)
- **Issue:** AppDbContext constructor agora requer ICurrentCompanyService, mas AppDbContextFactory não passava o parâmetro — CS7036
- **Fix:** Atualizado AppDbContextFactory para instanciar `new CurrentCompanyService()` e passar ao AppDbContext
- **Files modified:** src/Onboarding.Infrastructure/Persistence/AppDbContextFactory.cs
- **Verification:** `dotnet build` limpo — 0 erros 0 warnings
- **Committed in:** b60a229 (part of task commit)

---

**Total deviations:** 2 auto-fixed (1 bug, 1 blocking)
**Impact on plan:** Ambos essenciais para correção e compilação. Sem scope creep.

## Issues Encountered
- Nenhum — build limpo na primeira correção, migration gerada corretamente

## Next Phase Readiness
- Infrastructure layer completa: EF Core configs, repositories, migration prontos
- Plan 04 pode migrar Application layer (CQRS handlers, DTOs) e API Controllers do modelo antigo para Company/Employee/AccessGroup
- ICurrentCompanyService disponível para DI — Phase 39 fará o wiring JWT claims → CompanyId
- Migration preserva admin_audit_logs e password_reset_tokens (D-16) ✅

---
*Phase: 37-domain-model-redesign*
*Completed: 2026-04-26*