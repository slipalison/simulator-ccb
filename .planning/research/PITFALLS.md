# Domain Pitfalls — Fundos de Investimento Module

**Domain:** Adding investment fund cadastral management (Fundos, Cedentes, Custodiantes) to an existing .NET 10 DDD multi-tenant PJ onboarding system
**Project:** Onboarding de Clientes — v8.0+
**Researched:** 2026-05-02
**Overall confidence:** HIGH (codebase analysis + Brazilian regulatory knowledge + EF Core docs + Keycloak docs)

---

## Critical Pitfalls

Mistakes that cause data corruption, security breaches, or rewrites.

### PITFALL-01: Multi-Tenancy Leak Between Companies on Fund Data

**Risk:** 🔴 CRITICAL
**What goes wrong:** The existing system enforces company isolation via `HasQueryFilter` on `Employee` and `AccessGroup` using `ICurrentCompanyService.CompanyId`. When adding Fundo/Cedente/Custodiante entities, a developer forgets to add the same `HasQueryFilter` on the new entity configurations. Company A's PJ owner sees Company B's fund positions, exposure limits, or cedente relationships.
**Why it happens:** Each new aggregate requires its own `IEntityTypeConfiguration<T>` with `HasQueryFilter`. It's easy to create a new entity configuration file and forget the query filter line. The existing `CompanyConfiguration` does NOT have `HasQueryFilter` (Companies are accessed by their own sub claim), but `EmployeeConfiguration` and `AccessGroupConfiguration` both have it. New entities that belong to a company MUST have it.
**Consequences:** Cross-company data leak. LGPD violation (PII from another company). Regulatory compliance failure.
**Prevention:**
1. Every new EF Core entity configuration for company-scoped data MUST include `HasQueryFilter(e => e.CompanyId == _currentCompanyService.CompanyId)` in `Configure()`.
2. Create a code review checklist item: "Does this entity's configuration include HasQueryFilter?"
3. Write an integration test for EVERY new endpoint that verifies: Company A cannot read Company B's fund data. Follow the same pattern as `EmployeeConfiguration.HasQueryFilter`.
4. The `CompanyId` FK must be required (non-nullable) on every company-scoped entity. Nullable `CompanyId` on fund entities is a design smell — it means someone tried to make "global" funds that leak across tenants.
**Detection:** Integration test: authenticate as Company A user, call GET endpoint, verify Company B's data never appears. Run this for every new endpoint.

**Phase:** First domain layer phase (entity + configuration creation)

---

### PITFALL-02: CNPJ Alphanumeric Validation Breaking on July 2026

**Risk:** 🔴 CRITICAL
**What goes wrong:** The existing `Cnpj.cs` value object already handles alphanumeric CNPJ (ASCII-48 mapping, letter values A=17, B=18, etc.). However, the database column for `Cnpj` in `CompanyConfiguration` has `.HasMaxLength(14)` — this is correct for numeric CNPJ but WILL BREAK when alphanumeric CNPJs arrive (they're also 14 chars, so this is fine). But: if any fund entity (Cedente, Custodiante) reuses the `Cnpj` value object but stores it in a column with `.HasMaxLength(14)` on the *normalized* form, that works. The pitfall is in the *display* form and *search* form.
**Why it happens:** Starting July 2026, Receita Federal begins issuing alphanumeric CNPJs. Existing Cedentes/Custodiantes with numeric CNPJs remain valid forever, but NEW registrations may have letters (e.g., `12.ABC345/0001-90`). The check-digit algorithm must accept letters in positions 1-8 of the root, not just digits.
**Consequences:** Cedente/Custodiante registration fails for any new alphanumeric CNPJ. Data stored incorrectly. Existing numeric CNPJs break if the validation logic is "upgraded" incorrectly (e.g., rejecting all-numeric sequences that were valid before).
**Prevention:**
1. The existing `Cnpj.Create()` method already handles alphanumeric — verify it works for BOTH formats before building any new entity that uses CNPJ.
2. For the Fund module, Cedente and Custodiante WILL have CNPJs. Reuse the existing `Cnpj` value object directly. Do NOT create a separate `CnpjCedente` or `CnpjCustodiante`.
3. The database column for normalized CNPJ must be `VARCHAR(14)` (not `CHAR(14)`) because PostgreSQL `CHAR` pads with spaces. Use `varchar(14)` or `text` with a check constraint.
4. CNPJ *display* format changes: `XX.XXX.XXX/YYYY-ZZ` where any position 1-8 can be a letter. Stripping `.` `/` `-` before validation still works.
5. Write tests for: pure numeric CNPJ, mixed alphanumeric CNPJ, all-same-character CNPJ (rejected), wrong check digit CNPJ (rejected).
**Detection:** Unit tests in `Cnpj.cs` tests covering all cases. Integration test: register a Cedente with alphanumeric CNPJ, verify it persists and is queryable.

**Phase:** Domain layer phase (when creating Cnpj on new entities)

---

### PITFALL-03: Decimal Precision Loss on Monetary Values

**Risk:** 🔴 CRITICAL
**What goes wrong:** Fund entities have monetary fields: exposure limits, quota values, portfolio balances. Using `decimal` in C# with `.HasPrecision(18,2)` in EF Core means values are stored with 2 decimal places (e.g., R$ 1,234,567,890,123,456.78). For investment fund quotas, this is INSUFFICIENT — quotas often have 4-8 decimal places (e.g., R$ 1.00005234). Storing R$ 1.00005 as R$ 1.00 causes accumulation errors across billions in assets.
**Why it happens:** Developers default to `(18,2)` for "money" because that's standard currency format. But fund quotas and unit prices in Brazil require at least 8 decimal places. CVM (Comissão de Valores Mobiliários) regulations require specific decimal precision in reports.
**Consequences:** Accumulated rounding errors on portfolio values. Discrepancies between system reports and actual fund NAV (Net Asset Value). Regulatory non-compliance.
**Prevention:**
1. Define a `Money` value object with explicit `decimal` precision. Use `[Precision(18, 8)]` for quota values (8 decimal places). Use `[Precision(18, 2)]` for BRL currency amounts that are always whole centavos.
2. Better: create two value objects — `BrlAmount` (precision 18,2) for monetary amounts in Reais, and `QuotaValue` (precision 18,8) for quota unit prices. This makes the domain intent clear.
3. PostgreSQL `numeric(18,8)` stores exactly — no floating-point loss.
4. NEVER use `float` or `double` for monetary values — only `decimal` in C# and `numeric` in PostgreSQL.
5. Add FluentValidation rules that validate precision ranges per field.
**Detection:** Unit test: store a quota value of 1.00005234, read it back, assert exact equality. Test with maximum precision values.

**Phase:** Domain layer phase (when defining Fundo/Cedente aggregate with monetary fields)

---

### PITFALL-04: Status Transitions Without State Machine Enforcement

**Risk:** 🟠 HIGH
**What goes wrong:** Fund entities have a lifecycle: `RASCUNHO` → `ATIVO` → `ENCERRADO` → (dead end). A PJ admin accidentally sets an `ENCERRADO` fund back to `ATIVO`. Or a fund in `RASCUNHO` (draft) gets assigned cedentes before being activated. These invalid state transitions violate business rules.
**Why it happens:** Without a state machine or explicit transition enforcement, `Status` is just an enum that any API endpoint can set to any value. PUT /api/funds/{id} with `{ "status": "ATIVO" }` on an `ENCERRADO` fund succeeds because there's no validation.
**Consequences:** Regulatory violation (CVM prohibits reactivating closed funds without proper process). Data inconsistency (cedentes linked to funds in invalid states). Audit trail confusion.
**Prevention:**
1. Define `FundoStatus` as a value object or enum: `RASCUNHO`, `ATIVO`, `ENCERRADO`.
2. Implement explicit transition methods on the `Fundo` aggregate root:
   ```csharp
   public void Activate() { if (Status != FundoStatus.RASCUNHO) throw new DomainException(...); Status = FundoStatus.ATIVO; }
   public void Close() { if (Status != FundoStatus.ATIVO) throw new DomainException(...); Status = FundoStatus.ENCERRADO; }
   ```
3. NEVER allow direct `Status` setter from API. The `UpdateFundoCommand` should call domain methods, not set Status directly.
4. Add FluentValidation for status transitions in the Application layer as a guard rail (defense in depth).
5. Document valid transitions in domain comments or a state diagram in Mermaid.
**Detection:** Unit tests for each valid and invalid transition. Integration test: API returns 422 for invalid status transitions.

**Phase:** Domain layer phase (when creating Fundo aggregate)

---

### PITFALL-05: N-N Relationship with Payload Modeled as Simple Join Table

**Risk:** 🟠 HIGH
**What goes wrong:** A Cedente (assignor/seller) has a many-to-many relationship with Fundo (investment fund). But this isn't a simple N-N — the relationship carries data: `exposureLimit` (decimal), `assignedAt` (DateTime), `assignedBy` (Guid — the user who assigned). Modeling this as a simple `FundoCedente` join table with just `FundoId` + `CedenteId` loses the relationship's payload. Later someone adds columns ad-hoc, creating an anemic join entity.
**Why it happens:** EF Core's many-to-many convention for simple join tables is tempting. Developers add `.WithMany()` navigation properties and let EF create the join table automatically. Then they realize they need `exposureLimit` and scramble to add columns, but the domain model doesn't reflect the relationship's richness.
**Consequences:** Can't store exposure limits per cedente-per-fundo. Can't track when a cedente was assigned to a fund. Can't audit who assigned them. Data ambiguity (is this cedente active or inactive in this fund?).
**Prevention:**
1. Model `FundoCedente` as a full domain entity (not just a join table): it has its own `Id`, `FundoId`, `CedenteId`, `ExposureLimit`, `AssignedAt`, `AssignedBy`, and potentially `IsActive`, `RemovedAt`.
2. This entity lives in the Domain layer with invariants enforced (e.g., `ExposureLimit` must be > 0, `AssignedAt` cannot be future).
3. EF Core configuration maps it as an entity with two FK navigation properties (not a simple many-to-many).
4. The aggregate root (likely `Fundo`) owns the `FundoCedente` collection and enforces business rules like "can't assign same cedente twice".
5. Do NOT use EF Core's auto many-to-many — use explicit entity with `FundoCedenteConfiguration`.
**Detection:** Code review: if you see `builder.Entity<Fundo>().HasMany(f => f.Cedentes).WithMany()` without a join entity, it's wrong. The `FundoCedente` entity must have its own configuration file.

**Phase:** Domain layer phase (when creating Fundo ↔ Cedente relationship)

---

### PITFALL-06: Keycloak Permissions Not Scoped for Fund Management

**Risk:** 🟠 HIGH
**What goes wrong:** The existing permission system uses `Permissions.All` for company owners (PJ) and `AccessGroup.Permissions` for employees. If fund management endpoints use only `[Authorize(Policy = "EmployeeWrite")]`, any employee with `employees:write` permission can manage funds — even if they shouldn't. There's no `funds:read`, `funds:write`, `funds:manage` permission level.
**Why it happens:** The `Permissions` class has 6 existing permissions related to employees and access groups. Adding fund management without new permissions means piggybacking on existing permissions, which gives too much or too little access.
**Consequences:** Unauthorized users modify fund data. Over-privileged employees see sensitive financial data. Regulatory compliance violation (segregation of duties).
**Prevention:**
1. Add fund-related permissions to `Permissions.cs`:
   ```csharp
   public const string FundsRead = "funds:read";
   public const string FundsWrite = "funds:write";
   public const string FundsManage = "funds:manage"; // for status transitions, archive
   ```
2. Add these to `Permissions.All` array.
3. Add corresponding `PermissionPolicies` entries in `PermissionPolicyConstants.cs`.
4. Add `PermissionAuthorizationHandler` policy enforcements in `Program.cs` DI.
5. Default `AccessGroup.CreateDefaultGroups()` must be updated — `admin-empresa` gets all permissions including new fund ones; existing `viewer` and `dashboard` groups remain unchanged.
6. Migration must add these permissions to existing custom access groups or document that new permissions are opt-in.
7. Create an EF Core migration that adds the new permissions to the `permissions` JSON column in `access_groups` for the `admin-empresa` default group.
**Detection:** Integration test: Employee with `funds:read` only can GET but not POST/PUT/DELETE fund endpoints. Employee without `funds:*` gets 403.

**Phase:** First API phase (before building fund endpoints)

---

## Moderate Pitfalls

### PITFALL-07: Soft Delete vs Hard Delete for Regulated Fund Entities

**Risk:** 🟡 MEDIUM
**What goes wrong:** Investment fund data is regulated by CVM. You can't just DELETE a fund from the database — CVM requires historical data retention. But the existing pattern uses `DeletedAt` for LGPD compliance on Employee/Company. If you apply the same pattern to Fundo/Cedente, you get "anonymized" fund data that looks bizarre (fund named "Fundo Excluído" with zeroed exposure limit).
**Why it happens:** LGPD (right to be forgotten) conflicts with CVM (financial record retention). The `Anonymize()` pattern on `Company` and `Employee` nullifies PII but keeps the record. For a Cedente (which is a company), LGPD applies. For a Fundo's financial data, CVM retention applies.
**Prevention:**
1. **Cedente/Custodiante:** CAN be anonymized (LGPD applies — it's a company's personal data). Reuse the `Anonymize()` pattern from `Company`.
2. **Fundo (investment fund):** CANNOT be hard-deleted or anonymized. Use status transition `ENCERRADO` instead. An `ENCERRADO` fund is visible but read-only. This is the correct regulatory approach.
3. **`FundoCedente` (relationship):** Use soft-delete with `RemovedAt` rather than hard delete. This preserves the audit trail of which cedente was assigned to which fund and when.
4. Never implement `.Anonymize()` on Fundo — the fund's name and CNPJ must be retained even after closure. Only status changes from `ATIVO` → `ENCERRADO`.
**Detection:** Integration test: attempt to DELETE a fund, verify it returns 403 or 405. Verify ENCERRADO is the only "removal" path.

**Phase:** Domain layer phase (when defining entity lifecycle)

---

### PITFALL-08: Missing Audit Trail on Fund Management Actions

**Risk:** 🟡 MEDIUM
**What goes wrong:** The existing system has `AdminAuditLog` for admin actions, but fund management actions performed by PJ owners have NO audit trail. When a PJ owner changes a cedente's exposure limit from R$5M to R$50M, there's no record of who did it, when, or what the previous value was.
**Why it happens:** The current `AdminAuditLog` is designed for backoffice admin actions (admin managing users). Fund management is done by PJ company owners — not admins. There's no equivalent audit trail for company-user actions.
**Prevention:**
1. Create a `FundoAuditLog` entity (or generic `CompanyAuditLog`) that records: `CompanyId`, `ActionType` (enum: FundCreated, FundStatusChanged, CedenteAssigned, ExposureLimitChanged, etc.), `PerformedByUserId`, `PerformedByUserName`, `Timestamp`, `Details` (JSON or text), `IpAddress`.
2. This audit log follows the same append-only, immutable pattern as `AdminAuditLog` (no Update, no Delete methods).
3. Every command handler that modifies fund/cedente/custodiante data must append an audit record BEFORE committing changes.
4. Expose audit data via a read-only endpoint for PJ owners (within company isolation).
**Detection:** Integration test: perform a fund action, verify an audit record exists with correct `CompanyId`, `ActionType`, and `PerformedByUserId`.

**Phase:** Application layer phase (when implementing command handlers)

---

### PITFALL-09: CNPJ Uniqueness Across Company Boundaries

**Risk:** 🟡 MEDIUM
**What goes wrong:** The current system has a unique index on `Employee.Cpf` with `HasFilter("cpf IS NOT NULL")` and `Company.Cnpj` with `HasFilter("cnpj IS NOT NULL")`. Cedente entities also have CNPJs. If a Cedente's CNPJ must be unique ONLY within a company, the filter should be `WHERE cnpj IS NOT NULL AND company_id = @companyId`. If a Cedente's CNPJ must be globally unique (no two Cedentes in the ENTIRE system can have the same CNPJ), the filter is `WHERE cnpj IS NOT NULL`.
**Why it happens:** In the investment fund domain, a Cedente (assignor) is often a bank or financial institution. The same Cedente (Itaú, Bradesco) appears across MULTIPLE companies' fund positions. So a global unique CNPJ constraint on Cedente would prevent Company A and Company B from both having Itaú as a cedente.
**Consequences:** Either: (a) Global uniqueness prevents legitimate data entry, or (b) Per-company uniqueness allows duplicate Cedentes representing the same real-world entity, leading to data quality issues.
**Prevention:**
1. **Cedente is a GLOBAL entity, not a per-company entity.** Cedente (a bank/financial institution) exists independently of any company. Company A and Company B both reference the same Cedente record for Itaú.
2. Therefore: `Cedente` table has a GLOBAL unique index on `CNPJ`. The `FundoCedente` join table (relationship) is per-company.
3. This is different from `Employee.Cpf` which is globally unique (a person can't work at two companies in this system).
4. Design: `Cedente` is a shared reference entity. `FundoCedente` is the per-company relationship with exposure limits. `Custodiante` follows the same pattern (global entity, per-company relationship).
5. The `HasQueryFilter` for company isolation applies to `FundoCedente` (per-company), NOT to `Cedente` (global reference).
**Detection:** Integration test: create two companies, both create a fund referencing the same Cedente. Verify it succeeds (same Cedente, different FundoCedente records).

**Phase:** Domain layer phase (when defining Cedente/Custodiante as global vs company-scoped entities)

---

### PITFALL-10: Keycloak Service Account Scope Insufficient for Fund Roles

**Risk:** 🟡 MEDIUM
**What goes wrong:** The existing `onboarding-api-admin` Keycloak client has `manage-users` role. Adding fund management doesn't require new Keycloak roles for fund data (that's stored in PostgreSQL). But if the module adds "fund manager" as a Keycloak role for the PJ realm, and the service account can't manage those roles, adding/removing users from roles fails silently or returns 403.
**Why it happens:** Keycloak roles are separate from the application's `Permissions` system. The app has its own `AccessGroup.Permissions` list (`employees:read`, etc.) that lives in PostgreSQL. Keycloak manages authentication (who are you) while the app manages authorization (what can you do). Adding fund permissions to the app WITHOUT updating Keycloak means the authorization system works, but anyone trying to add Keycloak-level roles for fine-grained access will hit a wall.
**Prevention:**
1. **Do NOT add fund-specific Keycloak roles.** The existing pattern is correct: Keycloak authenticates (JWT token with `sub` claim), application authorizes (via `AccessGroup.Permissions`). Fund permissions (`funds:read`, `funds:write`) live in `Permissions.cs` and `access_groups.permissions` JSON column, NOT in Keycloak.
2. Add `funds:read`, `funds:write`, `funds:manage` to `Permissions.cs` and the `PermissionPolicyConstants.cs` authorization policies.
3. The `PermissionAuthorizationHandler` already resolves permissions from `ICurrentCompanyPermissionsService` — no Keycloak changes needed.
4. Only add Keycloak roles if you need OIDC-level claims in the JWT token for frontend routing decisions. Even then, the backend MUST still check application-level permissions.
**Detection:** Unit test: verify `[Authorize(Policy = "FundsRead")]` works with a user whose `AccessGroup` contains `funds:read` but whose Keycloak roles don't include any fund-specific role.

**Phase:** API endpoint phase (when building fund controller)

---

### PITFALL-11: FundoCedente Concurrent Modification Race Condition

**Risk:** 🟡 MEDIUM
**What goes wrong:** Two PJ admins from the same company simultaneously update the same cedente's exposure limit. One reads R$5M, changes to R$10M. The other also reads R$5M, changes to R$8M. Last write wins, the R$10M change is lost. No optimistic concurrency control.
**Why it happens:** The existing entities (`Company`, `Employee`, `AccessGroup`) don't use EF Core's optimistic concurrency token (`[Timestamp]` or `[ConcurrencyCheck]`). This works because only one admin at a time typically edits employee data. But fund exposure limits are sensitive financial data where concurrent edits are more likely.
**Prevention:**
1. Add a `[Timestamp]` (RowVersion) column to `FundoCedente` and `Fundo` entities. EF Core will throw `DbUpdateConcurrencyException` when a stale update is attempted.
2. Handle `DbUpdateConcurrencyException` in the command handler: return 409 Conflict with a meaningful message ("The exposure limit was modified by another user. Please refresh and try again.").
3. The frontend should display a clear conflict message and let the user re-submit with fresh data.
4. Alternative: use `[ConcurrencyCheck]` on specific columns (like `ExposureLimit`) instead of whole-row versioning. This gives more targeted conflict detection.
**Detection:** Integration test: two concurrent PUT requests on the same FundoCedente, verify one succeeds and one returns 409.

**Phase:** Application layer phase (when implementing update command handlers)

---

### PITFALL-12: CVM Regulatory Status Names Hardcoded in English

**Risk:** 🟡 MEDIUM
**What goes wrong:** Brazilian fund status names (`RASCUNHO`, `ATIVO`, `ENCERRADO`) are coded as English enums (`Draft`, `Active`, `Closed`). Regulatory documents always reference the Portuguese terms. When generating CVM-compliant reports or integrating with external systems (CVM, BACEN), the English terms don't map properly.
**Why it happens:** Developers default to English for code identifiers. But in this domain, the regulatory vocabulary IS Portuguese. A "closed" fund in English might mean `ENCERRADO` or `LIQUIDADO` — they're different statuses in CVM regulation.
**Prevention:**
1. Use Portuguese terms for enum values in the domain: `Rascunho`, `Ativo`, `Encerrado`. These are domain terms that map 1:1 to regulatory language.
2. Alternatively, use explicit string constants: `FundoStatus.RASCUNHO`, `FundoStatus.ATIVO`, `FundoStatus.ENCERRADO`.
3. Map to user-friendly display names in the Application/DTO layer (can be Portuguese or English depending on UI requirements).
4. API responses should use the Portuguese terms in status fields — this matches what CVM and Brazilian financial professionals expect.
5. Do NOT invent English translations (like "Draft" for `RASCUNHO`) that have no regulatory equivalent.
**Detection:** Code review: enum values must match CVM status terminology. No English-only status values.

**Phase:** Domain layer phase (when defining FundoStatus enum)

---

## Minor Pitfalls

### PITFALL-13: Pagination Missing on Fund Listing Endpoints

**Risk:** 🟢 LOW
**What goes wrong:** The existing employee listing has pagination (`GetCompanyEmployeesQuery` with `page`, `pageSize`, `search`, `status`). If fund listing endpoints don't implement pagination from day one, a company with 5000 funds returns a 5MB JSON response.
**Prevention:** Follow the `PaginatedResult<T>` pattern already in the codebase. Every list endpoint for Fundo, Cedente, Custodiante, and FundoCedente MUST accept `page` and `pageSize` parameters. Default `pageSize` should be 20.

**Phase:** API endpoint phase

---

### PITFALL-14: FundoCedente Exposure Limit Stored as Nullable Instead of Required

**Risk:** 🟢 LOW
**What goes wrong:** `ExposureLimit` on `FundoCedente` is modeled as `decimal?` (nullable). This creates ambiguity: does `null` mean "no limit" (unlimited exposure) or "not yet set" (data incomplete)?
**Prevention:** Use a sentinel value like `decimal.MaxValue` for "unlimited exposure" and require `ExposureLimit > 0` for all other cases. Make `ExposureLimit` non-nullable `decimal` in the domain entity. Or create a `ExposureLimit` value object that encodes the "unlimited" concept explicitly.

**Phase:** Domain layer phase

---

### PITFALL-15: Forgetting to Include Fund Entities in AppDbContext

**Risk:** 🟢 LOW
**What goes wrong:** New `Fundo`, `Cedente`, `Custodiante`, `FundoCedente` entities are created in the Domain layer but never registered as `DbSet<T>` in `AppDbContext`. EF Core throws `InvalidOperationException` at runtime: "The entity type 'Fundo' was not found."
**Prevention:** Add `public DbSet<Fundo> Fundos => Set<Fundo>();` (and same for other entities) to `AppDbContext.cs`. Add corresponding `IEntityTypeConfiguration<T>` to `OnModelCreating`. Follow the existing pattern — `CompanyConfiguration`, `EmployeeConfiguration`, `AccessGroupConfiguration`.

**Phase:** Infrastructure phase (after domain entities are defined)

---

### PITFALL-16: Frontend Fund Forms Allow Invalid Status Transitions

**Risk:** 🟢 LOW
**What goes wrong:** Backend correctly validates status transitions (PITFALL-04), but the frontend dropdown shows ALL statuses regardless of current state. User sees "Active" as an option on an already-closed fund, selects it, gets a 422 error, and is confused.
**Prevention:** Frontend must compute available next statuses based on current status. If `status === 'ENCERRADO'`, no transition options. If `status === 'RASCUNHO'`, only `ATIVO` is available. This is a UX concern, not a security concern (backend still validates).

**Phase:** Frontend phase (when building fund detail/edit pages)

---

### PITFALL-17: Missing Indexes on Fund Query Patterns

**Risk:** 🟢 LOW
**What goes wrong:** Fund listing filtered by status, cedente search by CNPJ, or fundo-cedente query by company + fund are all full table scans on large datasets. Performance degrades linearly.
**Prevention:** Add indexes in EF Core configuration:
- `Fundo`: index on `CompanyId`, `Status`
- `Cedente`: unique index on `Cnpj`
- `Custodiante`: unique index on `Cnpj`
- `FundoCedente`: composite index on `(FundoId, CedenteId)`, index on `CompanyId`

**Phase:** Infrastructure phase

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Domain entities (Fundo, Cedente, Custodiante) | PITFALL-02, PITFALL-03, PITFALL-04, PITFALL-05, PITFALL-12 | Define value objects (BrlAmount, QuotaValue), status transition methods, N-N-with-payload entity, Portuguese enum names |
| EF Core configuration + migration | PITFALL-01, PITFALL-09, PITFALL-15, PITFALL-17 | HasQueryFilter on ALL company-scoped entities, global uniqueness for Cedente CNPJ, DbSet registration, strategic indexes |
| Application layer (command handlers) | PITFALL-08, PITFALL-11 | Audit log for every mutation, optimistic concurrency on FundoCedente |
| API endpoints (controllers) | PITFALL-06, PITFALL-13 | New permission policies for fund actions, pagination on all list endpoints |
| Frontend (fund management UI) | PITFALL-16 | Compute available status transitions based on current status |
| Integration with existing Employee/Company system | PITFALL-01, PITFALL-06, PITFALL-10 | Multi-tenancy filter on every new entity, new fund permissions, Keycloak unchanged |
| Keycloak integration | PITFALL-10 | Do NOT add fund-specific Keycloak roles — use existing application-level permission system |
| Regulatory compliance (CVM) | PITFALL-04, PITFALL-07, PITFALL-12 | Status state machine, ENCERRADO instead of delete, Portuguese terminology |

---

## Integration Pitfalls with Existing System

### Existing Multi-Tenancy Pattern Must Be Extended

The current system uses `ICurrentCompanyService.CompanyId` + `HasQueryFilter` for company isolation. Every new company-scoped entity MUST follow this exact pattern:

1. `CompanyId` property on the entity (non-nullable `Guid`)
2. `HasQueryFilter(e => e.CompanyId == _currentCompanyService.CompanyId)` in the entity configuration
3. `ICurrentCompanyService` is resolved per-request in `ClientClaimsMiddleware`
4. Controller checks `companyId != _currentCompanyService.CompanyId` → `Forbid()`

**Fundo, FundoCedente** are company-scoped → MUST have this pattern.
**Cedente, Custodiante** are global reference entities → MUST NOT have this pattern (they're shared across companies).

### Existing Audit Pattern Must Be Extended

The current `AdminAuditLog` is specifically for backoffice admin actions. Fund management actions by PJ owners need their own audit trail. Create `CompanyAuditLog` or `FundoAuditLog` following the same immutable, append-only pattern but scoped to `CompanyId`.

### Existing Permission System Must Be Extended

Add `funds:read`, `funds:write`, `funds:manage` to `Permissions.cs`. Add to `PermissionPolicyConstants.cs`. Add to `Program.cs` policy registration. Add to `AccessGroup.CreateDefaultGroups()` for `admin-empresa`.

### Existing CNPJ Value Object Is Reusable

The existing `Cnpj.cs` value object already handles alphanumeric validation. Reuse it for Cedente/Custodiante entities. Do NOT duplicate validation logic.

### Existing Cpf Value Object Pattern Applies

If the fund module needs CPF for individual cedentes (person as cedente), the existing `Cpf.cs` value object is reusable.

---

## Sources

- [EF Core HasQueryFilter — Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/querying/filters) — HIGH confidence (official docs)
- [EF Core Decimal Precision — Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties) — HIGH confidence (official docs)
- [Keycloak Admin REST API](https://www.keycloak.org/docs-api/latest/rest-api/index.html) — HIGH confidence (official)
- [CNPJ Alphanumeric Format 2026 — Wikipedia pt-BR](https://pt.wikipedia.org/wiki/Cadastro_Nacional_da_Pessoa_Jur%C3%ADdica) — HIGH confidence (official Receita Federal announcement referenced)
- [CNPJ Verification — Commenda](https://www.commenda.io/blog/brazil-cnpj-verification) — MEDIUM confidence (commercial source, verified against Wikipedia)
- [CVM Instruction 558/2015 — Fund Regulation](https://conteudo.cvm.gov.br/legislacao/instruc/inst558.html) — HIGH confidence (official CVM regulation)
- [BACEN Resolution 4,943/2022 — Financial Institution Registration](https://www.bcb.gov.br/estabilidadefinanceira/resolucao_4943) — MEDIUM confidence (regulatory reference)
- Existing codebase analysis — HIGH confidence (first-party source)