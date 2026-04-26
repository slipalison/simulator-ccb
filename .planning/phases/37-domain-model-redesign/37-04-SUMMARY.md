---
phase: 37-domain-model-redesign
plan: 04
subsystem: api, application
tags: [cqrs, company, employee, dto, controller, admin, migration, ddd, stub-handlers]

# Dependency graph
requires:
  - phase: 37-domain-model-redesign
    plan: 01
    provides: Company, Employee, AccessGroup, Permissions, TermsAcceptance domain types + ICompanyRepository/IEmployeeRepository/IAccessGroupRepository interfaces
  - phase: 37-domain-model-redesign
    plan: 03
    provides: EF Core configs, HasQueryFilter, repositories, ICurrentCompanyService, migration
provides:
  - Application layer CQRS/DTOs migrated from User→Company/Employee (D-19)
  - AdminUserController migrated to Company/Employee endpoints
  - CompaniesController with /me and /registration skeleton
  - Stub handlers for Phase 38/41 full implementation
  - Zero references to Client/User CQRS in Application and API layers
  - Clean solution build (0 errors, 0 warnings)
affects: [38-employee-registration, 41-backoffice-employee]

# Tech tracking
tech-stack:
  added: []
  patterns: [stub-handlers-with-notimplementedexception, company-employee-admin-endpoints]

key-files:
  created:
    - src/Onboarding.Application/Admin/Commands/UpdateCompanyCommand.cs
    - src/Onboarding.Application/Admin/Commands/DeleteEmployeeCommand.cs
    - src/Onboarding.Application/Admin/Commands/BlockEmployeeCommand.cs
    - src/Onboarding.Application/Admin/Commands/UnblockEmployeeCommand.cs
    - src/Onboarding.Application/Admin/Queries/GetPaginatedCompaniesQuery.cs
    - src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs
    - src/Onboarding.Application/Admin/Queries/GetCompanyDetailsQuery.cs
    - src/Onboarding.Application/Admin/Queries/GetEmployeeDetailsQuery.cs
    - src/Onboarding.Application/Admin/DTOs/CompanySummaryDto.cs
    - src/Onboarding.Application/Admin/DTOs/EmployeeSummaryDto.cs
  modified:
    - src/Onboarding.Application/DependencyInjection.cs
    - src/Onboarding.API/Controllers/AdminUserController.cs
    - src/Onboarding.API/Controllers/CompaniesController.cs
    - src/Onboarding.API/Program.cs
  deleted:
    - src/Onboarding.Application/Clients/ (entire directory)
    - src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs
    - src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs
    - src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs
    - src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs
    - src/Onboarding.Application/Admin/Queries/GetPaginatedUsersQuery.cs
    - src/Onboarding.Application/Admin/Queries/GetUserDetailsQuery.cs
    - src/Onboarding.Application/Admin/DTOs/UserSummaryDto.cs
    - src/Onboarding.Application/Admin/DTOs/UserDetailDto.cs
    - src/Onboarding.Application/Admin/DTOs/UpdateUserRequest.cs
    - src/Onboarding.Application/Admin/Validators/UpdateUserCommandValidator.cs
    - src/Onboarding.Application/Admin/Validators/BlockUserCommandValidator.cs
    - src/Onboarding.Application/Admin/Validators/UnblockUserCommandValidator.cs
    - src/Onboarding.Application/Admin/Validators/DeleteUserCommandValidator.cs
    - src/Onboarding.API/Controllers/RegistrationController.cs
    - src/Onboarding.API/Controllers/RegisterClientRequest.cs

key-decisions:
  - "Stub handlers com NotImplementedException para Company/Employee commands/queries — implementacao completa na Phase 38/41"
  - "AdminUserController endpoints /api/admin/users removidos — substituidos por /api/admin/companies e /api/admin/employees"
  - "CompaniesController com skeleton /registration endpoint (retorna 501) — fluxo completo na Phase 38"
  - "Testes de API obsoletos deletados junto com codigo — serao reescritos na Phase 38/41"

patterns-established:
  - "Stub handler pattern: handler que throw NotImplementedException com mensagem indicando fase futura de implementacao"
  - "Admin endpoint migration: User→Company endpoints em /api/admin/companies, Employee endpoints em /api/admin/employees"

requirements-completed: [REG-02, REG-04, REG-05]

# Metrics
duration: 12min
completed: 2026-04-26
---

# Phase 37 Plan 04: Application & API Migration Summary

**Migracao completa Application layer + API controllers de User/Client para Company/Employee — zero referencias obsoletas, solution build limpo**

## Performance

- **Duration:** 12 min
- **Started:** 2026-04-26T02:22:44Z
- **Completed:** 2026-04-26T02:34:14Z
- **Tasks:** 2
- **Files modified:** 44 (10 created, 4 modified, 16 deleted in Application; 2 modified, 2 deleted in API; 10 test files modified/deleted)

## Accomplishments
- Diretorio Clients/ inteiro deletado — RegisterClientCommand, Handler, Validator, Result removidos (D-19)
- Commands User deletados: UpdateUserCommand, DeleteUserCommand, BlockUserCommand, UnblockUserCommand + handlers
- Queries User deletados: GetPaginatedUsersQuery, GetUserDetailsQuery + handlers
- DTOs User deletados: UserSummaryDto, UserDetailDto, UpdateUserRequest
- Validators User deletados: UpdateUserCommandValidator, BlockUserCommandValidator, UnblockUserCommandValidator, DeleteUserCommandValidator
- Novos DTOs: CompanySummaryDto (Id, RazaoSocial, Cnpj, Email, Phone, IsDeleted, KeycloakUserId), EmployeeSummaryDto (Id, Nome, Cpf, Email, Phone, CompanyId, CompanyRazaoSocial, AccessGroupId, AccessGroupName, IsDeleted, KeycloakUserId)
- Novos commands Company/Employee com stub handlers (NotImplementedException para Phase 38/41)
- Novas queries Company/Employee com stub handlers (NotImplementedException para Phase 38/41)
- AdminUserController migrado: endpoints /api/admin/companies e /api/admin/employees substituem /api/admin/users
- CompaniesController atualizado com skeleton /registration endpoint (501 Not Implemented — Phase 38)
- RegistrationController e RegisterClientRequest deletados
- DependencyInjection.cs atualizado: removidos registros Client/User, adicionados Company/Employee
- Program.cs: comentario atualizado (removida referencia a ClientRepository)
- 7 testes de API obsoletos deletados + 3 testes atualizados para novos endpoints
- dotnet build: 0 erros, 0 warnings — solution inteira compila
- 124 testes domain passando, 46 testes Company/Employee/AccessGroup/Permissions passando

## Task Commits

1. **Task 1: Delete obsolete Client CQRS/DTOs + create Company/Employee replacements** - `87cd6ac` (feat)
2. **Task 2: Migrate controllers + update Program.cs DI + full build verification** - `f3d9841` (feat)

## Files Created/Modified

### Application Layer
- `src/Onboarding.Application/Admin/Commands/UpdateCompanyCommand.cs` - Command + stub handler para atualizar empresa
- `src/Onboarding.Application/Admin/Commands/DeleteEmployeeCommand.cs` - Command + stub handler para deletar funcionario (LGPD)
- `src/Onboarding.Application/Admin/Commands/BlockEmployeeCommand.cs` - Command + stub handler para bloquear funcionario
- `src/Onboarding.Application/Admin/Commands/UnblockEmployeeCommand.cs` - Command + stub handler para desbloquear funcionario
- `src/Onboarding.Application/Admin/Queries/GetPaginatedCompaniesQuery.cs` - Query + stub handler para listagem paginada de empresas
- `src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs` - Query + stub handler para listagem paginada de funcionarios
- `src/Onboarding.Application/Admin/Queries/GetCompanyDetailsQuery.cs` - Query + stub handler para detalhes de empresa
- `src/Onboarding.Application/Admin/Queries/GetEmployeeDetailsQuery.cs` - Query + stub handler para detalhes de funcionario
- `src/Onboarding.Application/Admin/DTOs/CompanySummaryDto.cs` - DTO resumo de empresa (substitui UserSummaryDto)
- `src/Onboarding.Application/Admin/DTOs/EmployeeSummaryDto.cs` - DTO resumo de funcionario (novo)
- `src/Onboarding.Application/DependencyInjection.cs` - Registrados handlers Company/Employee, removidos Client/User

### API Layer
- `src/Onboarding.API/Controllers/AdminUserController.cs` - Migrado para endpoints Company/Employee
- `src/Onboarding.API/Controllers/CompaniesController.cs` - Adicionado skeleton /registration endpoint (501)
- `src/Onboarding.API/Program.cs` - Comentario limpo (sem referencia a ClientRepository)

### Deleted Files
- `src/Onboarding.Application/Clients/` (entire directory — 4 files)
- `src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs` - Handler obsoleto
- `src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs` - Handler obsoleto
- `src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs` - Handler obsoleto
- `src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs` - Handler obsoleto
- `src/Onboarding.Application/Admin/Queries/GetPaginatedUsersQuery.cs` - Handler obsoleto
- `src/Onboarding.Application/Admin/Queries/GetUserDetailsQuery.cs` - Handler obsoleto
- `src/Onboarding.Application/Admin/DTOs/UserSummaryDto.cs` - DTO obsoleto
- `src/Onboarding.Application/Admin/DTOs/UserDetailDto.cs` - DTO obsoleto
- `src/Onboarding.Application/Admin/DTOs/UpdateUserRequest.cs` - DTO obsoleto
- `src/Onboarding.Application/Admin/Validators/` (4 User validator files)
- `src/Onboarding.API/Controllers/RegistrationController.cs` - Controller obsoleto
- `src/Onboarding.API/Controllers/RegisterClientRequest.cs` - Request DTO obsoleto

### Test Files
- `tests/Onboarding.API.Tests/Admin/AdminUserControllerTests.cs` - Atualizado para Company/Employee handlers
- `tests/Onboarding.API.Tests/Admin/AdminAuthorizationTests.cs` - Atualizado para endpoints /api/admin/companies, /api/admin/employees
- `tests/Onboarding.API.Tests/Admin/AdminUserDetailsTests.cs` - Renomeado para AdminCompanyDetailsTests, usa CompanySummaryDto
- `tests/Onboarding.Domain.Tests/Application/AdminValidatorTests.cs` - DELETADO (testava validadores User obsoletos)
- `tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs` - DELETADO (testava RegistrationController obsoleto)
- `tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs` - DELETADO (testava /api/registration obsoleto)
- `tests/Onboarding.API.Tests/Api/RegistrationErrorPathTests.cs` - DELETADO (testava RegistrationController obsoleto)
- `tests/Onboarding.API.Tests/Admin/AdminFullFlowTests.cs` - DELETADO (testava fluxo User obsoleto)
- `tests/Onboarding.API.Tests/Admin/AdminUserBlockTests.cs` - DELETADO (testava /api/admin/users/{id}/block obsoleto)
- `tests/Onboarding.API.Tests/Admin/AdminUserDeleteTests.cs` - DELETADO (testava /api/admin/users/{id} DELETE obsoleto)
- `tests/Onboarding.API.Tests/Admin/AdminUserListingTests.cs` - DELETADO (testava /api/admin/users GET obsoleto)
- `tests/Onboarding.API.Tests/Admin/AdminUserUpdateTests.cs` - DELETADO (testava /api/admin/users/{id} PUT obsoleto)

## Decisions Made
- Stub handlers com NotImplementedException para adiar implementacao completa — mensagens indicam "Full implementation in Phase 38/41"
- Testes de API obsoletos deletados ao inves de migrados — serao reescritos na Phase 38/41 quando handlers forem implementados
- CompaniesController /registration retorna 501 (Not Implemented) ao inves de 503 — indica falta de implementacao, nao erro de servico
- AdminUserController mantem Phase 35 admin management endpoints (administrators, toggle-status, reset-password) intactos

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Testes de API referenciando tipos deletados causam build failure**
- **Found during:** Task 2 (build do projeto de testes)
- **Issue:** AdminValidatorTests.cs, RegistrationControllerTests.cs, IdempotencyFilterTests.cs, RegistrationErrorPathTests.cs, AdminUserDetailsTests.cs, AdminUserBlockTests.cs, AdminUserDeleteTests.cs, AdminUserListingTests.cs, AdminUserUpdateTests.cs, AdminFullFlowTests.cs referenciavam UpdateUserCommand, BlockUserCommand, GetPaginatedUsersQuery, UserSummaryDto, UserDetailDto, RegisterClientCommand, RegistrationController — todos deletados
- **Fix:** Deletados 7 testes obsoletos (fluxo User/Registration), atualizados 3 testes (AdminUserControllerTests, AdminAuthorizationTests, AdminUserDetailsTests) para usar Company/Employee equivalents
- **Files modified:** 10 test files (7 deletados, 3 atualizados)
- **Verification:** `dotnet build` de todos projetos — 0 erros, 0 warnings
- **Committed in:** f3d9841 (Task 2 commit)

**2. [Rule 1 - Bug] Faltava using FluentValidation no AdminUserController**
- **Found during:** Task 2 (primeiro build do API project)
- **Issue:** IValidator<> nao resolvido — faltava `using FluentValidation;` no arquivo reescrito
- **Fix:** Adicionado `using FluentValidation;` no topo do AdminUserController.cs
- **Files modified:** src/Onboarding.API/Controllers/AdminUserController.cs
- **Verification:** `dotnet build` — 0 erros
- **Committed in:** f3d9841 (part of task commit)

---

**Total deviations:** 2 auto-fixed (1 missing critical, 1 bug)
**Impact on plan:** Ambos essenciais para compilação e correção. Sem scope creep.

## Known Stubs
- `UpdateCompanyCommandHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `DeleteEmployeeCommandHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `BlockEmployeeCommandHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `UnblockEmployeeCommandHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `GetPaginatedCompaniesHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `GetPaginatedEmployeesHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `GetCompanyDetailsHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `GetEmployeeDetailsHandler.HandleAsync()` — throws NotImplementedException, Phase 38/41
- `CompaniesController.RegisterCompany()` — returns 501 Not Implemented, Phase 38

## Next Phase Readiness
- Application layer 100% migrado: zero referencias a Client/User/PF
- API controllers 100% migrados: AdminUserController opera sobre Company/Employee
- CompaniesController com skeleton /registration (fluxo completo na Phase 38)
- Build limpo 0 erros 0 warnings — base solida para Phase 38
- Admin integration tests precisam ser reescritos na Phase 38/41 (handlers stubs retornam NotImplementedException)
- ICurrentCompanyService registrado via Infrastructure DI — disponivel para HasQueryFilter

## Self-Check: PASSED

- All 12 created files verified on disk
- 3 deleted directories/files confirmed removed
- Both commits (87cd6ac, f3d9841) verified in git log
- dotnet build entire solution: 0 errors, 0 warnings
- 124 domain tests passing, 46 new-entity tests passing

---
*Phase: 37-domain-model-redesign*
*Completed: 2026-04-26*