# Phase 43: E2E Playwright Validation — Validation Strategy

**Phase:** 43-e2e-playwright-validation
**Created:** 2026-04-26
**Source:** Derived from 43-RESEARCH.md Validation Architecture section

---

## Per-Requirement Automated Commands

| Req ID | Behavior | Test Type | Automated Command | Plan |
|--------|----------|-----------|-------------------|------|
| E2E-01 | Cadastro PJ completo → auto-login → rota padrão | e2e | `npx playwright test --project=registration registration` | 43-01 |
| E2E-02 | Dashboard exibe cards mock | e2e | `npx playwright test --project=dashboard dashboard` | 43-02 |
| E2E-03 | PJ cria funcionário → aparece na lista | e2e | `npx playwright test --project=admin-empresa employee-management` | 43-02 |
| E2E-04 | Login como funcionário → redirect por group | e2e | `npx playwright test --project=employee-login employee-login` | 43-03 |
| E2E-05 | JWT decode + permission UI | e2e | `npx playwright test --project=viewer permission-ui && npx playwright test --project=admin-empresa permission-ui` | 43-03 |
| E2E-06 | Change group → re-login → updated permissions | e2e | `npx playwright test --project=admin-empresa access-group-change` | 43-03 |
| E2E-07 | All tests pass | e2e | `npx playwright test` | All |

## Sampling Rate

- **Per task commit:** `npx playwright test --project=<relevant-project>`
- **Per wave merge:** `npx playwright test`
- **Phase gate:** Full suite green with `npx playwright test` before `/gsd-verify-work`

## Wave 0 Gaps

- [ ] `playwright.config.ts` — Playwright configuration file
- [ ] `e2e/auth/*.setup.ts` — Auth setup files (2: admin-empresa, viewer)
- [ ] `e2e/pages/*.page.ts` — Page Object files (5: keycloak-login, registration, dashboard, employees, profile)
- [ ] `e2e/fixtures/test-data.ts` — Test data generators (CNPJ/CPF with check digits)
- [ ] `e2e/fixtures/jwt-utils.ts` — JWT decode utilities (jose)
- [ ] `e2e/*.spec.ts` — Test spec files (6: registration, dashboard, employee-management, employee-login, permission-ui, access-group-change)
- [ ] `playwright/.auth/` — Storage state directory (gitignored)
- [ ] @playwright/test in package.json devDependencies
- [ ] Chromium browser install: `npx playwright install chromium`
- [ ] .gitignore entry for `playwright/.auth/`, `test-results/`, `playwright-report/`

## Security Validation

| ASVS Category | Applies | Validation |
|---------------|---------|------------|
| V2 Authentication | yes | ACF + PKCE full redirect flow validated in E2E-01, E2E-04 |
| V3 Session Management | yes | httpOnly cookie session validated in E2E-05 (JWT from cookie) |
| V4 Access Control | yes | Group-based permission rendering validated in E2E-05 (viewer vs admin-empresa UI) |

---

*Phase: 43-e2e-playwright-validation*
*Validation strategy created: 2026-04-26*