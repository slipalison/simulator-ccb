---
phase: 37-domain-model-redesign
plan: 02
subsystem: testing
tags: [tdd, xunit, shouldly, company, employee, access-group, permissions, terms-acceptance, unit-tests]

# Dependency graph
requires:
  - phase: 37-domain-model-redesign
    provides: Company, Employee, AccessGroup, Permissions, TermsAcceptance domain types
provides:
  - 45 testes unitários organizados em 5 arquivos para todos os novos tipos do domain
  - Cobertura completa: Register, Anonymize, Update, SetKeycloakUserId, CreateDefaultGroups, Permissions constants
  - Zero referências a Client/PF nos testes (D-20)
affects: [38-employee-registration, 42-ci-coverage]

# Tech tracking
tech-stack:
  added: []
  patterns: [separate-test-file-per-entity, flat-aggregates-test-namespace]

key-files:
  created:
    - tests/Onboarding.Domain.Tests/Aggregates/CompanyTests.cs
    - tests/Onboarding.Domain.Tests/Aggregates/TermsAcceptanceTests.cs
    - tests/Onboarding.Domain.Tests/Aggregates/EmployeeTests.cs
    - tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs
    - tests/Onboarding.Domain.Tests/Aggregates/PermissionsTests.cs
  modified: []
  deleted:
    - tests/Onboarding.Domain.Tests/Aggregates/CompanyAggregate/CompanyTests.cs
    - tests/Onboarding.Domain.Tests/Aggregates/EmployeeAggregate/EmployeeTests.cs

key-decisions:
  - "Testes separados em arquivos individuais — CompanyTests, TermsAcceptanceTests, EmployeeTests, AccessGroupTests, PermissionsTests"
  - "Namespace plano Onboarding.Domain.Tests.Aggregates — sem subdiretórios por aggregate"

patterns-established:
  - "1 arquivo de teste por entidade/VO — sem misturar classes no mesmo arquivo"
  - "Namespaces flat em Aggregates/ — segue estrutura da spec do plano"

requirements-completed: [REG-02, REG-04, REG-05]

# Metrics
duration: 8min
completed: 2026-04-25
---

# Phase 37 Plan 02: Domain Model Tests Summary

**45 testes unitários em 5 arquivos separados cobrindo Company, Employee, TermsAcceptance, AccessGroup, Permissions — zero referências a Client/PF**

## Performance

- **Duration:** 8 min
- **Started:** 2026-04-25T22:56:16Z
- **Completed:** 2026-04-25T23:04:00Z
- **Tasks:** 1
- **Files modified:** 7 (5 created, 2 deleted)

## Accomplishments
- Reorganização de testes domain em arquivos separados por entidade (5 arquivos, 45 testes)
- CompanyTests: 9 testes — Register (valid/nulo/vazio/invalido), Anonymize (idempotente), SetKeycloakUserId, Update
- TermsAcceptanceTests: 8 testes — Create (AcceptedAt, TermsVersion, IpAddress), validação nulo/vazio, CurrentVersion
- EmployeeTests: 12 testes — Register (sets CompanyId/AccessGroupId), Anonymize (idempotente), SetAccessGroup, Update, SetKeycloakUserId
- AccessGroupTests: 9 testes — Create, CreateDefaultGroups (3 grupos verificados individualmente), UpdatePermissions (valido/invalido)
- PermissionsTests: 7 testes — All.Length=6, cada constante com verificação de valor string
- Subdiretórios CompanyAggregate/ e EmployeeAggregate/ removidos — namespace flat
- Zero referências a Client, ClientType.PF, RegisterPessoaFisica nos testes (D-20)
- 139 testes passando (45 novos domain + 94 existentes)

## Task Commits

1. **Task 1: Write tests for Company, TermsAcceptance, Employee, AccessGroup, Permissions + delete Client tests** - `f3dc346` (test)

## Files Created/Modified
- `tests/Onboarding.Domain.Tests/Aggregates/CompanyTests.cs` - 9 testes Company: Register, Anonymize, Update, SetKeycloakUserId
- `tests/Onboarding.Domain.Tests/Aggregates/TermsAcceptanceTests.cs` - 8 testes TermsAcceptance: Create, validação, CurrentVersion
- `tests/Onboarding.Domain.Tests/Aggregates/EmployeeTests.cs` - 12 testes Employee: Register, Anonymize, SetAccessGroup, Update
- `tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs` - 9 testes AccessGroup: Create, CreateDefaultGroups, UpdatePermissions
- `tests/Onboarding.Domain.Tests/Aggregates/PermissionsTests.cs` - 7 testes Permissions: All.Length, cada constante
- `tests/Onboarding.Domain.Tests/Aggregates/CompanyAggregate/CompanyTests.cs` - DELETADO (substituído)
- `tests/Onboarding.Domain.Tests/Aggregates/EmployeeAggregate/EmployeeTests.cs` - DELETADO (substituído)

## Decisions Made
- Arquivos de teste separados por entidade ao invés de agrupados — melhora legibilidade e manutenção
- Namespace plano `Onboarding.Domain.Tests.Aggregates` — sem subdiretórios por aggregate, seguindo spec do plano

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Assertion incorreta Phone.Value após Anonymize**
- **Found during:** Task 1 (CompanyTests)
- **Issue:** Teste esperava `"+0000000000"` mas `PhoneNumber.Create` remove o prefixo `+`, resultando em `"0000000000"`
- **Fix:** Corrigida assertion para `company.Phone.Value.ShouldBe("0000000000")` — alinhado com o comportamento real de `PhoneNumber.Create`
- **Files modified:** tests/Onboarding.Domain.Tests/Aggregates/CompanyTests.cs
- **Verification:** dotnet test — 139/139 passando
- **Committed in:** f3dc346

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Correção de assertion — sem impacto no comportamento do domain.

## Issues Encountered
- Testes de Client/PF já haviam sido removidos no plano 01 — nenhuma ação necessária para D-20

## Next Phase Readiness
- 45 testes domain organizados e passando — base sólida para Fase 38 (Employee Registration & Management API)
- Cobertura TDD completa para Register, Anonymize, Update, Permissions
- Fase 38 pode adicionar testes de integração/confirmação sem conflitos

## Self-Check: PASSED

- All 6 created files verified on disk
- 2 deleted files confirmed removed
- Commit f3dc346 verified in git log
- 139/139 tests passing

---
*Phase: 37-domain-model-redesign*
*Completed: 2026-04-25*