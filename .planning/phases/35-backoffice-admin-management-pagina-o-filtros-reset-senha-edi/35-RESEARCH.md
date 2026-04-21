# Phase 35: Admin Management Backend - Research

## Domain Findings
1. **Existing Controller**: `AdminUserController.cs` already possesses base admin scaffolding. `GetAdministrators` endpoint exists but returns unpaginated `IReadOnlyList<AdminUserDto>`.
2. **Missing Endpoints**: We need `PUT /api/admin/administrators/{id}` (Update), `POST /api/admin/administrators/{id}/reset-password`, `POST /api/admin/administrators/{id}/deactivate`, `POST /api/admin/administrators/{id}/reactivate`.

## Technical Architectural Plan
- **Pagination & Filters**: Convert `GetAdministratorsQuery` to return `PaginatedResult<AdminUserDto>` and accept `page`, `pageSize`, `search` (name/email), and `status`.
- **Update Admin**: Create `UpdateAdministratorCommand`. Require validation to ensure email uniqueness (SEC-04). Must block updating own account (SEC-01). Inject `IAuditService` to log old vs new values (AUD-04).
- **Reset Password**: Create `ResetAdministratorPasswordCommand`. Block modifying self (SEC-01). Generate 16-char crypto-secure random string without ambiguous characters. Update in Keycloak as temporary password and mark `UPDATE_PASSWORD` action. Audit log action but NEVER the password (AUD-05).
- **Deactivate/Reactivate**: Create `DeactivateAdministratorCommand` and `ReactivateAdministratorCommand`. Deactivate must check if it's the last active admin (SEC-05), prohibit self-deactivation. Must force immediate session revocation (`logoutAll` in Keycloak). Log actor, target, reason if provided (AUD-06).

## Validation Architecture
- **Tests Needed**: Unit tests for all commands to verify SEC-01 to SEC-05. Specifically asserting that `ArgumentException` or `InvalidOperationException` is thrown for self-modification.
- **Keycloak Mocking**: Need to ensure `IKeycloakUserService` is mocked correctly to assert session disablement and password generation logic.

## RESEARCH COMPLETE
