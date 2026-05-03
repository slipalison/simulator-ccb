# Phase 46: Infrastructure Layer v8.0 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-03
**Phase:** 46-infrastructure-layer
**Areas discussed:** CedenteDocumento DU Persistence, FundoCedente Unique Constraint, Repository Pattern

---

## CedenteDocumento DU Persistence

| Option | Description | Selected |
|--------|-------------|----------|
| Shadow properties com discriminator | 3 columns: documento_tipo, cpf (nullable), cnpj_cedente (nullable). HasConversion maps DU. Consistent with Employee.Cpf pattern. | ✓ |
| JSON column | Single JSONB column with {"type": "PF", "value": "..."}. Simpler code but harder uniqueness queries, violates implicit "no JSON for cadastral data" pattern. | |
| Owned entity | OwnsOne(e => e.Documento, ...) with EF Core discriminator. More verbose, no advantage over shadow properties. | |

**User's choice:** Shadow properties com discriminator (D-09)
**Notes:** Columns named `documento_tipo`, `cpf`, `cnpj_cedente` to avoid collision with other Cnpj columns. Filtered composite indexes for CAD-18 uniqueness per company.

---

## FundoCedente Unique Constraint (REL-09)

| Option | Description | Selected |
|--------|-------------|----------|
| Partial unique index | `HasIndex(fc => new { fc.FundoId, fc.CedenteId }).HasFilter("status = 1")`. PostgreSQL native. Allows multiple inactive (history), guarantees unique active. | ✓ |
| Domain enforcement only | Fundo.AddCedente() already rejects. Risk: race condition between check and insert. Not recommended for financial data. | |

**User's choice:** Partial unique index (D-11)
**Notes:** ATIVO = enum value 1. Matches existing pattern where enum stored as integer.

---

## Repository Pattern — Admin Bypass

| Option | Description | Selected |
|--------|-------------|----------|
| Follow EmployeeRepository pattern | IgnoreQueryFilters() for GetById and admin queries. Explicit CompanyId for listing. Same thin wrapper approach. | ✓ |
| Separate admin repository | Create IAdminFundoRepository. Over-engineering — admin queries are just IgnoreQueryFilters. | |

**User's choice:** Follow EmployeeRepository pattern (D-12)
**Notes:** Admin methods (GetPagedAllAsync, GetByIdIgnoreFilterAsync) deferred to Phase 48 when AdminFundosController is created. Phase 46 delivers only the minimum contracts from existing repository interfaces.

---

## Claude's Discretion

- Column naming conventions (snake_case, following existing pattern)
- DI registration order in DependencyInjection.cs
- ApplyConfiguration order in AppDbContext.OnModelCreating
- Index naming conventions (IX_tablename_column pattern)
- Folder structure for repositories (following existing Repositories/ pattern)