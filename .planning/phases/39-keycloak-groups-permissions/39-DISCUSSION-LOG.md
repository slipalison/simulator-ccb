# Phase 39: Keycloak Groups & Permissions - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-26
**Phase:** 39-keycloak-groups-permissions
**Areas discussed:** JWT Claims & Authorization Policy, PJ-to-Employee Group Assignment Flow, Company Isolation Defense-in-Depth

---

## JWT Claims — Group-to-JWT Mapping

| Option | Description | Selected |
|--------|-------------|----------|
| Groups as Keycloak Realm Roles | Add admin-empresa/viewer/dashboard as realm roles. Reuse RealmRolesClaimsTransformation. Zero new mappers. | |
| Groups as Separate JWT Claim | Add Group Membership mapper → groups claim. New GroupsClaimsTransformation. | |
| Dual Claims (roles + groups) | Backoffice realm keeps realm_access.roles. Client realm adds groups claim. Each realm native. | ✓ |

**User's choice:** Dual Claims — backoffice realm keeps realm_access.roles, client realm adds groups claim via Group Membership mapper.
**Notes:** Semantic separation — groups = company access groups, roles = system roles.

---

## Permission Resolution — Group → Permissions

| Option | Description | Selected |
|--------|-------------|----------|
| Group Name → DB Permissions | JWT groups claim → AccessGroup name → DB lookup → permission list. Flexible. | ✓ |
| Group Name → Direct Role Check | [Authorize(Roles = "admin-empresa")]. Rigid — permission changes need code redeploy. | |

**User's choice:** Group Name → DB Permissions. Most flexible — permission changes in DB don't require code/Keycloak changes.

---

## CurrentCompanyService Wiring

| Option | Description | Selected |
|--------|-------------|----------|
| JWT → DB Lookup | Middleware parses JWT sub → queries Company by KeycloakUserId → sets CompanyId + permissions. Single source of truth. | ✓ |
| JWT Custom Claim | Extract companyId from JWT custom claim. Requires Keycloak custom attribute + mapper. No DB lookup but larger token. | |

**User's choice:** JWT → DB Lookup via new ClientClaimsMiddleware.

---

## Dual Claims — Per-Realm Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Each realm uses its native claim type | Backoffice = realm_access.roles (existing). Client = groups claim (new). Clear separation. | ✓ |
| Groups-only in client, roles only in backoffice | Both realms use groups claim. Requires refactoring backoffice (out of scope). | |

**User's choice:** Each realm uses its native claim type.

---

## Company Lookup Middleware

| Option | Description | Selected |
|--------|-------------|----------|
| New ClientClaimsMiddleware | Runs after UseAuthentication. Reads JWT sub, queries DB, sets ICurrentCompanyService. | ✓ |
| Expand ClientSessionMiddleware | Mix cookie extraction with company resolution. Different concerns merged. | |

**User's choice:** New middleware.

---

## Admin Bypass Authorization Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Role-based + IgnoreQueryFilters | [Authorize(Roles = "admin")] + .IgnoreQueryFilters(). Clean, simple. | |
| Policy-based authorization | [Authorize(Policy = "CrossCompanyAccess")]. Declarative, extensible. | ✓ |

**User's choice:** Policy-based authorization.

---

## Authorization Policy Granularity

| Option | Description | Selected |
|--------|-------------|----------|
| Single policy requiring admin role | options.AddPolicy("CrossCompanyAccess", policy => policy.RequireRole("admin")). | |
| Granular per-permission policies | 6 policies (EmployeeRead, EmployeeWrite, EmployeeDelete, AuditRead, DashboardAccess, AccessGroupsManage) + CrossCompanyAccess. | ✓ |

**User's choice:** Granular per-permission policies — one policy per resource:action (7 total including CrossCompanyAccess).

---

## Areas Not Discussed (Skipped by User)

- **PJ-to-Employee Group Assignment Flow** — deferred to agent's discretion. Key decisions already captured: DB update + Keycloak group sync (AddUserToGroup/RemoveUserFromGroup), eventual consistency on failure.
- **Company Isolation Defense-in-Depth** — deferred to agent's discretion. HasQueryFilter + controller check + service-layer check pattern already established in Phase 37/38.

## Agent's Discretion

- Exact method signatures for IKeycloakUserService group operations
- Keycloak Group ID ↔ AccessGroup mapping strategy (name-based lookup vs DB storage)
- GroupsClaimsTransformation implementation details
- Middleware pipeline ordering
- Edge case handling (multiple groups, deleted groups, stale Keycloak state)

---

*Discussion log generated: 2026-04-26*