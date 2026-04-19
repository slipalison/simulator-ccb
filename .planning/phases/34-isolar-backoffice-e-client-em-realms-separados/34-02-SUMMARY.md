# Plan 34-02 Summary: Backend Authentication Pipeline

**Status:** Completed
**Validation:** `Program.cs` dual JWT Bearer scheme applied. `ClientsController` and `AdminUserController` adjusted to exclusively enforce their respective authentication schemas.

- Modified `Program.cs` to remove single `JwtBearerDefaults.AuthenticationScheme` and add `BearerBackoffice` and `BearerClient` schemes, securely validating audiences and extracting Keycloak roles only for the Backoffice.
- Fixed `AdminUserController` to use `BearerBackoffice` schema.
- Fixed `ClientsController` to use `BearerClient` schema.

**Files modified:**
- `src/Onboarding.API/Program.cs`
- `src/Onboarding.API/Controllers/AdminUserController.cs`
- `src/Onboarding.API/Controllers/ClientsController.cs`
