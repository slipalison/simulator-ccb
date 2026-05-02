---
phase: 37-domain-model-redesign
plan: 01
subsystem: database, api
tags: [ddd, ef-core, company, employee, access-group, terms-acceptance, permissions, lgpd]

# Dependency graph
requires:
  - phase: 03-backend-domain-layer
    provides: Value objects (Cpf, Cnpj, Email, PhoneNumber), Entity base, Client aggregate
provides:
  - Company aggregate root with Register() factory and TermsAcceptance value object
  - Employee aggregate root with CompanyId FK and AccessGroupId
  - AccessGroup entity with CreateDefaultGroups() and permission validation
  - Permissions static class with 6 resource:action constants
  - ICompanyRepository, IEmployeeRepository, IAccessGroupRepository interfaces
  - DuplicateCompanyException (renamed from DuplicateClientException)
  - ActionType enum extended with values 18–25
affects: [38-employee-registration, 39-keycloak-groups, 40-client-frontend, 41-backoffice-employee]

# Tech tracking
tech-stack:
  added: []
  patterns: [company-aggregate-root, employee-aggregate-root, access-group-entity, terms-acceptance-vo, permissions-constants]

key-files:
  created:
    - src/Onboarding.Domain/Aggregates/CompanyAggregate/Company.cs
    - src/Onboarding.Domain/Aggregates/CompanyAggregate/TermsAcceptance.cs
    - src/Onboarding.Domain/Aggregates/EmployeeAggregate/Employee.cs
    - src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs
    - src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs
    - src/Onboarding.Domain/Repositories/ICompanyRepository.cs
    - src/Onboarding.Domain/Repositories/IEmployeeRepository.cs
    - src/Onboarding.Domain/Repositories/IAccessGroupRepository.cs
    - src/Onboarding.Domain/Exceptions/DuplicateCompanyException.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs
    - src/Onboarding.Infrastructure/Repositories/CompanyRepository.cs
    - src/Onboarding.API/Controllers/CompaniesController.cs
  modified:
    - src/Onboarding.Infrastructure/Persistence/AppDbContext.cs
    - src/Onboarding.Infrastructure/Repositories/AdminRepository.cs
    - src/Onboarding.Infrastructure/DependencyInjection.cs
    - src/Onboarding.Domain/Aggregates/Audit/ActionType.cs
    - src/Onboarding.Domain/Repositories/IAdminRepository.cs

key-decisions:
  - "Company substitui Client como aggregate root PJ — nenhuma mistura PF/PJ"
  - "TermsAcceptance como owned type (EF Core OwnsOne) na tabela companies"
  - "AccessGroup.CreateDefaultGroups() retorna 3 grupos: admin-empresa, viewer, dashboard"
  - "Permissions constantes com pattern resource:action — 6 permissões predefinidas"
  - "Permissions alias (Perm) em AccessGroup para evitar colisão de nomes com propriedade List"

patterns-established:
  - "Company aggregate: Register() factory com TermsAcceptance obrigatório (D-12)"
  - "Employee aggregate: Register() factory com CompanyId Guid FK sem navigation (D-02)"
  - "AccessGroup: CreateDefaultGroups() seeding pattern para novas empresas"
  - "Permissions validation: UpdatePermissions() rejeita permissões não predefinidas (T-37-04)"

requirements-completed: [REG-02, REG-04, REG-05]

# Metrics
duration: 35min
completed: 2026-04-25
---

# Phase 37 Plan 01: Domain Model Redesign Summary

**Company + Employee aggregates com TermsAcceptance, AccessGroup e Permissions substituem Client obsoleto — base para Fase 38+**

## Performance

- **Duration:** 35 min
- **Started:** 2026-04-25T22:29:43Z
- **Completed:** 2026-04-25T23:05:00Z
- **Tasks:** 2
- **Files modified:** 47

## Accomplishments
- Company aggregate root com Register(razaoSocial, cnpj, email, phone, termsAcceptance) — TermsAcceptance obrigatório (D-03, D-12)
- Employee aggregate root com CompanyId Guid FK, AccessGroupId, Anonymize() LGPD
- AccessGroup entity com CreateDefaultGroups(3 grupos), UpdatePermissions() com validação (T-37-04)
- TermsAcceptance value object com Create(), CurrentVersion="1.0", AcceptedAt=UtcNow
- Permissions: 6 constantes employees:read/write/delete, audit:read, dashboard:access, access-groups:manage
- Client aggregate e IClientRepository completamente removidos (D-19)
- ActionType estendido com CompanyRegistered=18 a AccessGroupChanged=25
- Downstream: Application, Infrastructure, API, testes atualizados Client→Company
- 35 testes unitários passando (17 Company + 18 Employee/AccessGroup/Permissions)

## Task Commits

1. **Task 1: Company aggregate + TermsAcceptance + repositories + delete Client** - `063fec9` (feat)
2. **Task 2: Employee aggregate + AccessGroup + repositories + extend ActionType** - `c522d76` (feat)

## Files Created/Modified
- `src/Onboarding.Domain/Aggregates/CompanyAggregate/Company.cs` - Company aggregate root com Register() e Anonymize()
- `src/Onboarding.Domain/Aggregates/CompanyAggregate/TermsAcceptance.cs` - Value object com AcceptedAt, TermsVersion, IpAddress
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Employee.cs` - Employee aggregate root com CompanyId FK
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` - Entidade com Permissions e CreateDefaultGroups()
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs` - 6 constantes resource:action
- `src/Onboarding.Domain/Repositories/ICompanyRepository.cs` - ExistsByCnpjAsync (REG-02)
- `src/Onboarding.Domain/Repositories/IEmployeeRepository.cs` - GetPagedByCompanyAsync
- `src/Onboarding.Domain/Repositories/IAccessGroupRepository.cs` - AddRangeAsync, GetByCompanyIdAsync
- `src/Onboarding.Domain/Exceptions/DuplicateCompanyException.cs` - Renomeado de DuplicateClientException
- `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` - Valores 18-25 adicionados
- `src/Onboarding.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs` - Substitui ClientConfiguration
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` - DbSet<Company> Companies
- `src/Onboarding.Infrastructure/Repositories/CompanyRepository.cs` - Substitui ClientRepository
- `src/Onboarding.API/Controllers/CompaniesController.cs` - Substitui ClientsController
- `tests/Onboarding.Domain.Tests/Aggregates/CompanyAggregate/CompanyTests.cs` - 17 testes Company + TermsAcceptance
- `tests/Onboarding.Domain.Tests/Aggregates/EmployeeAggregate/EmployeeTests.cs` - 18 testes Employee + AccessGroup + Permissions

## Decisions Made
- Alias `using Perm = ...Permissions` em AccessGroup para evitar colisão de nomes entre propriedade `List<string> Permissions` e classe estática `Permissions`
- Downstream (Application, Infrastructure, API, testes) atualizados junto com Domain — necessário para poder compilar e rodar testes
- IAdminRepository migrado de Client para Company — GetPagedAsync e AdminRepository adaptados

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] IAdminRepository e downstream references Client→Company**
- **Found during:** Task 1 (Company aggregate creation)
- **Issue:** Após deletar Client aggregate, IAdminRepository, RegisterClientCommandHandler, AppDbContext, ClientRepository, AdminUserController, testes de API não compilavam
- **Fix:** Atualizados todos os downstream files: IAdminRepository→Company, ClientRepository→CompanyRepository, ClientConfiguration→CompanyConfiguration, ClientsController→CompaniesController, testes de API PJ-only
- **Files modified:** ~30 arquivos em Application, Infrastructure, API, testes
- **Verification:** `dotnet build` sem erros, 35 testes passando
- **Committed in:** 063fec9 (Task 1 commit)

**2. [Rule 1 - Bug] Permissions property name collision in AccessGroup**
- **Found during:** Task 2 (AccessGroup implementation)
- **Issue:** `Permissions.All` resolvia para `Enumerable.All()` ao invés da constante estática — colisão de nomes
- **Fix:** Criado alias `using Perm = ...Permissions` para desambiguar referências à classe estática
- **Files modified:** src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs
- **Verification:** Domain build OK, 18 testes passando
- **Committed in:** c522d76 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 bug)
**Impact on plan:** Ambos essenciais para compilação e funcionalidade. Sem scope creep.

## Issues Encountered
- Test project references Application+Infrastructure que dependiam de Client — exigiu atualização cascata completa (esperado por D-19/D-20)

## Known Stubs
- RegisterClientCommand/Validator/Request ainda usam campos PF/PJ (cpf, nome) — serão substituídos na Fase 38 por CompanyRegistrationCommand
- EmployeeAccessGroup.cs listado nos files_modified do plano mas não criado — não necessário, AccessGroup é entidade independente

## Next Phase Readiness
- Domain model completo: Company, Employee, AccessGroup, Permissions, TermsAcceptance prontos
- Repositórios de interface definidos — implementação EF Core completa para Company, faltam Employee e AccessGroup (Fase 38)
- ActionType estendido — pronto para auditoria de ações Company/Employee
- Migração EF Core (drop `clients`, criar `companies`, `employees`, `access_groups`) — Fase 38
- Handlers CQRS para registro PJ e gestão de funcionários — Fase 38
- Keycloak groups sync para AccessGroups — Fase 39

---
*Phase: 37-domain-model-redesign*
*Completed: 2026-04-25*