# Phase 44: Custom Access Groups CRUD

**Goal:** PJ pode criar, editar e deletar grupos de acesso customizados com permissões granulares (resource:action), em vez de ficar limitado aos 3 grupos fixos (admin-empresa, viewer, dashboard). Sistema extensível para crescimento.

**Depends on:** Phase 40 (Client Frontend)
**Requirements:** PERM-04 (extended), PERM-06

---

## Context

Currently, access groups are seeded automatically when a Company registers (admin-empresa, viewer, dashboard). PJ owners can only assign employees to these 3 fixed groups. This is too rigid for real-world use — companies need custom groups like "financeiro" (employees:read + audit:read) or "gerente" (employees:read + employees:write + audit:read).

The domain model already supports this:
- `AccessGroup` entity has `Name` (string) + `Permissions` (List<string>)
- `Permissions.All` defines the canonical list: `employees:read`, `employees:write`, `employees:delete`, `audit:read`, `dashboard:access`, `access-groups:manage`
- `AccessGroup.Create(companyId, name, permissions)` factory method exists
- `AccessGroup.UpdatePermissions(permissions)` with validation against `Permissions.All`

What's missing: API endpoints and frontend UI for CRUD operations.

---

## Success Criteria

1. **POST** `/api/companies/{companyId}/access-groups` creates a custom access group with name + permissions — only users with `access-groups:manage` permission can create
2. **PUT** `/api/companies/{companyId}/access-groups/{id}` updates name and/or permissions of a custom access group — default groups (admin-empresa, viewer, dashboard) CANNOT be edited or deleted
3. **DELETE** `/api/companies/{companyId}/access-groups/{id}` deletes a custom access group — rejects deletion of default groups; rejects deletion if employees are linked (returns 400 with suggestion to move employees first)
4. **Frontend**: "Grupos de Acesso" page with table listing all groups (default + custom), "Novo Grupo" button (visible only with `access-groups:manage`), edit/delete actions (disabled for default groups)
5. **Create dialog**: name input + permission checkboxes (employees:read, employees:write, employees:delete, audit:read, dashboard:access, access-groups:manage) — validation: name required, at least 1 permission
6. **Edit dialog**: pre-fills name + current permissions; same validation rules
7. **Delete confirmation**: if employees are linked, shows count and suggests moving them first; if no employees linked, confirms deletion
8. **RegisterEmployeeDialog** already fetches groups from `GET /api/companies/{companyId}/access-groups` — custom groups appear automatically

---

## Implementation Plan

### Tasks

#### Backend

1. **CreateAccessGroupCommand + Handler + Validator**
   - `CreateAccessGroupCommand(Guid CompanyId, string Name, List<string> Permissions)`
   - Validates: name not empty, permissions subset of `Permissions.All`, name unique within company
   - Creates `AccessGroup.Create(companyId, name, permissions)`
   - Syncs to Keycloak: creates Keycloak group in realm `client` via `IKeycloakUserService.CreateGroupAsync`
   - Audit: `ActionType.AccessGroupCreated`

2. **UpdateAccessGroupCommand + Handler + Validator**
   - `UpdateAccessGroupCommand(Guid CompanyId, Guid AccessGroupId, string? Name, List<string>? Permissions)`
   - Validates: group exists, group is NOT a default group (name != admin-empresa, viewer, dashboard), permissions subset of `Permissions.All`
   - Calls `AccessGroup.UpdatePermissions(permissions)` or updates name
   - Syncs to Keycloak: updates Keycloak group name if changed
   - Audit: `ActionType.AccessGroupUpdated`

3. **DeleteAccessGroupCommand + Handler**
   - `DeleteAccessGroupCommand(Guid CompanyId, Guid AccessGroupId)`
   - Validates: group exists, group is NOT a default group, no employees linked (check `IEmployeeRepository.ExistsByAccessGroupIdAsync`)
   - Deletes group from DB, optionally deletes Keycloak group
   - Audit: `ActionType.AccessGroupDeleted`

4. **IEmployeeRepository.ExistsByAccessGroupIdAsync(Guid accessGroupId)** — new method

5. **CompaniesController endpoints**
   - `POST /api/companies/{companyId}/access-groups` — authorize `access-groups:manage`
   - `PUT /api/companies/{companyId}/access-groups/{id}` — authorize `access-groups:manage`
   - `DELETE /api/companies/{companyId}/access-groups/{id}` — authorize `access-groups:manage`
   - All validate `companyId == _currentCompanyService.CompanyId`

6. **ActionType enum extensions**: AccessGroupCreated, AccessGroupUpdated, AccessGroupDeleted

#### Frontend

7. **API functions** (`api.ts`)
   - `createAccessGroup(companyId, { name, permissions })`
   - `updateAccessGroup(companyId, accessGroupId, { name?, permissions? })`
   - `deleteAccessGroup(companyId, accessGroupId)`
   - `PERMISSIONS` constant array for UI labels

8. **AccessGroupsPage.tsx**
   - Table with columns: Nome, Permissões (badges), Ações (edit/delete)
   - "Novo Grupo" button (visible only with `access-groups:manage`)
   - Default groups show lock icon + disabled actions
   - Edit opens CreateAccessGroupDialog in edit mode

9. **CreateAccessGroupDialog.tsx**
   - Name input + permission checkboxes with labels
   - Validation: name required, at least 1 permission
   - Delete confirmation with employee count warning

10. **Route + Sidebar**
    - `/access-groups` route in router
    - "Grupos de Acesso" entry in Sidebar (visible with `access-groups:manage`)

---

## Key Decisions

- **Default groups are immutable**: admin-empresa, viewer, dashboard cannot be renamed, have permissions changed, or deleted. This prevents accidental lockout.
- **Custom groups can have any combination of permissions**: a "financeiro" group with `employees:read` + `audit:read` is valid
- **Delete with employees linked**: returns 400 with error message suggesting move employees first. No silent reassignment.
- **Keycloak sync**: custom groups are created as Keycloak groups in realm `client`. When assigned to an employee, `RegisterEmployeeCommand` or `ChangeAccessGroupCommand` adds the user to the Keycloak group.

---

## Permissions Available

| Permission | Label (PT-BR) | Description |
|-----------|---------------|-------------|
| `employees:read` | Ver funcionários | Can view employee list and details |
| `employees:write` | Gerenciar funcionários | Can register, edit employees |
| `employees:delete` | Excluir funcionários | Can LGPD-delete employees |
| `audit:read` | Ver auditoria | Can view audit log |
| `dashboard:access` | Acesso ao dashboard | Can view dashboard page |
| `access-groups:manage` | Gerenciar grupos | Can create, edit, delete access groups |

## Default Groups (Immutable)

| Group | Permissions |
|-------|------------|
| admin-empresa | ALL permissions |
| viewer | employees:read, audit:read |
| dashboard | dashboard:access |