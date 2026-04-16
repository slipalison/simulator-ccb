# 30-02 Summary: Admin Administrators Endpoints

## Status: COMPLETE

## Task 1: AdminUserDto + GetAdministratorsQuery + GetUsersByRoleAsync

### Files modified/created
- `src/Onboarding.Application/Common/IAuditService.cs` — Added `AdminUserDto` record (Id, Email, FullName, IsEnabled, HasTemporaryPassword)
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — Added `GetUsersByRoleAsync` method signature
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — Implemented `GetUsersByRoleAsync` calling Keycloak Admin API `GET /admin/realms/{realm}/roles/{roleName}/users`, with `BuildFullName` helper and `KeycloakUserRepresentation` record
- `src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs` — Created query record and handler
- `src/Onboarding.Application/DependencyInjection.cs` — Registered `GetAdministratorsQueryHandler`
- `tests/Onboarding.Domain.Tests/Application/Commands/GetAdministratorsQueryHandlerTests.cs` — Created unit tests (2 tests passing)

### Verification
- `dotnet build src/Onboarding.API` — exits 0
- `dotnet test tests/Onboarding.Domain.Tests --filter "GetAdministratorsQueryHandlerTests"` — 2/2 passing

## Task 2: AdminUserController + Frontend Update

### Files modified
- `src/Onboarding.API/Controllers/AdminUserController.cs` — 
  - Renamed POST route from `[HttpPost("users")]` to `[HttpPost("administrators")]` for CreateAdmin
  - Added `GET /api/admin/administrators` endpoint (`GetAdministrators` action)
  - Added `_administratorsHandler` field and constructor injection
- `frontend/backoffice/src/lib/admin-api.ts` — Updated `createAdmin` function URL from `/api/admin/users` to `/api/admin/administrators`
- `tests/Onboarding.API.Tests/Admin/AdminAuthorizationTests.cs` — Added 4 new tests:
  - `GetAdministrators_WithNonAdminToken_ReturnsForbidden`
  - `GetAdministrators_WithAdminToken_ReturnsOk`
  - `CreateAdmin_WithNonAdminToken_ReturnsForbidden`
  - `CreateAdmin_OldRoute_ReturnsMethodNotAllowed`

### Verification
- `dotnet build src/Onboarding.API` — exits 0
- `dotnet test tests/Onboarding.API.Tests --filter "AdminAuthorizationTests"` — 8/8 passing

## Commits
1. `feat(30-02): add AdminUserDto, GetAdministratorsQuery, and GetUsersByRoleAsync`
2. `feat(30-02): rename CreateAdmin route to /administrators, add GET /administrators, update frontend`

## Acceptance Criteria Met
- `AdminUserDto` record exists in `IAuditService.cs` with Id, Email, FullName, IsEnabled, HasTemporaryPassword
- `IKeycloakUserService.GetUsersByRoleAsync` exists with correct signature
- `GetAdministratorsQuery.cs` exists with query record and handler
- `KeycloakUserService` implements `GetUsersByRoleAsync` with `requiredActions` mapping
- `GetAdministratorsQueryHandler` registered in `DependencyInjection.cs`
- `AdminUserController` has `[HttpPost("administrators")]` for CreateAdmin
- `AdminUserController` has `[HttpGet("administrators")]` for GetAdministrators
- `admin-api.ts` uses `/api/admin/administrators` in `createAdmin` function
- All unit and integration tests pass
