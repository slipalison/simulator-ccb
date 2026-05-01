# Phase 44: Custom Access Groups CRUD — Summary

**Status:** ✅ COMPLETE
**Date:** 2026-05-01 (validated from codebase)

---

## What Was Delivered

PJ pode criar, editar e deletar grupos de acesso customizados com permissões granulares — além dos 3 grupos fixos (admin-empresa, viewer, dashboard). Sistema extensível para crescimento.

---

## Implementation Details

### Backend

1. **CreateAccessGroupCommand + Handler + Validator** — `CreateAccessGroupCommandHandler.cs`
   - Validates: name not empty, permissions subset of `Permissions.All`, name unique within company
   - Creates `AccessGroup.Create(companyId, name, permissions)`
   - Syncs to Keycloak: creates group in realm `client` via `IKeycloakUserService.CreateGroupAsync`
   - Audit: `ActionType.AccessGroupCreated` (27)

2. **UpdateAccessGroupCommand + Handler + Validator** — `UpdateAccessGroupCommandHandler.cs`
   - Validates: group exists, NOT a default group (admin-empresa, viewer, dashboard), permissions subset of `Permissions.All`
   - Updates name and/or permissions
   - Syncs to Keycloak: updates group name if changed
   - Audit: `ActionType.AccessGroupUpdated` (28)

3. **DeleteAccessGroupCommand + Handler** — `DeleteAccessGroupCommandHandler.cs`
   - Validates: group exists, NOT a default group, no employees linked (`IEmployeeRepository.ExistsByAccessGroupIdAsync`)
   - Returns 400 if employees linked with suggestion to move employees first
   - Deletes from DB + Keycloak
   - Audit: `ActionType.AccessGroupDeleted` (29)

4. **IEmployeeRepository.ExistsByAccessGroupIdAsync** — implemented in `EmployeeRepository.cs`

5. **CompaniesController endpoints:**
   - POST `/api/companies/{companyId}/access-groups` — authorize `access-groups:manage`
   - PUT `/api/companies/{companyId}/access-groups/{id}` — authorize `access-groups:manage`
   - DELETE `/api/companies/{companyId}/access-groups/{id}` — authorize `access-groups:manage`
   - All validate `companyId == _currentCompanyService.CompanyId`

6. **ActionType enum:** AccessGroupCreated (27), AccessGroupUpdated (28), AccessGroupDeleted (29)

7. **AccessGroupDto:** `record AccessGroupDto(Guid Id, string Name, IReadOnlyList<string> Permissions, bool IsDefault = false)`

### Frontend

8. **API functions** (`api.ts`):
   - `createAccessGroup(companyId, { name, permissions })`
   - `updateAccessGroup(companyId, accessGroupId, { name?, permissions? })`
   - `deleteAccessGroup(companyId, accessGroupId)`
   - `PERMISSION_OPTIONS` constant array with value + label
   - `PERMISSION_LABELS` record for PT-BR display names

9. **AccessGroupsPage.tsx** — table listing all groups (default + custom)
   - "Novo Grupo" button (visible only with `access-groups:manage`)
   - Default groups show Lock icon + edit/delete disabled
   - Custom groups show edit/delete buttons
   - Create/Edit dialog with name input + permission checkboxes
   - Delete confirmation dialog with employee-linked warning

10. **Route + Sidebar:**
    - `/access-groups` route in router
    - "Grupos de Acesso" entry in Sidebar (visible with `access-groups:manage`)

---

## Success Criteria Verification

| # | Criteria | Status |
|---|----------|--------|
| 1 | POST creates custom access group with name + permissions, only `access-groups:manage` | ✅ |
| 2 | PUT updates name/permissions, default groups CANNOT be edited | ✅ |
| 3 | DELETE rejects default groups + groups with employees linked (400) | ✅ |
| 4 | Frontend page with table + Novo Grupo button (permission-gated) | ✅ |
| 5 | Create dialog: name input + permission checkboxes, validation | ✅ |
| 6 | Edit dialog: pre-fills current values, same validation | ✅ |
| 7 | Delete confirmation: employee count warning, suggests moving first | ✅ |
| 8 | RegisterEmployeeDialog fetches all groups (default + custom) | ✅ |

---

## Key Decisions

- Default groups are immutable — prevents accidental lockout
- Custom groups can have any combination of permissions
- Delete with employees linked returns 400 — no silent reassignment
- Keycloak sync: custom groups created as Keycloak groups in realm `client`

---

## Permissions Available

| Permission | Label (PT-BR) |
|-----------|---------------|
| `employees:read` | Ver funcionários |
| `employees:write` | Gerenciar funcionários |
| `employees:delete` | Excluir funcionários |
| `audit:read` | Ver auditoria |
| `dashboard:access` | Acesso ao dashboard |
| `access-groups:manage` | Gerenciar grupos |

## Default Groups (Immutable)

| Group | Permissions |
|-------|------------|
| admin-empresa | ALL permissions |
| viewer | employees:read, audit:read |
| dashboard | dashboard:access |