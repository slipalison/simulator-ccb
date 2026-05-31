# Phase 55 — controller-di-reduction — GATE

Gate D-62: every controller ≤ 5 ctor deps.

| Controller | Deps before | Deps after | Gate |
|---|---|---|---|
| `FundosController` | **37** | **4** (ICommandDispatcher + IQueryDispatcher + IValidationRunner + ICurrentCompanyService) | PASS |
| `AdminUserController` | **23** | **5** (+ IKeycloakUserService + ILogger) | PASS |
| `CompaniesController` | **17** | **5** (+ ICurrentCompanyService + ILogger) | PASS |
| `AdminFundosController` | **11** | **1** (IQueryDispatcher only) | PASS |
| `FundoCedentesController` | **8** | **4** | PASS |
| `FundoTiposAtivosController` | **8** | **4** | PASS |
| `CedenteTiposAtivosController` | **8** | **4** | PASS |
| `AuthController` | **8** | **3** (ICommandDispatcher + IValidationRunner + ILogger) | PASS |
| `PermissionsController` | **1** | **1** (no CQRS handlers — gate exception documented in WARNINGS.md) | PASS |
