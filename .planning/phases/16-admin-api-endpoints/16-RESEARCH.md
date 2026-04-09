# Phase 16 Research: Admin API Endpoints

## Standard Stack

### Packages Already in Use (No Addition Needed)
| Package | Version | Role in Phase 16 |
|---------|---------|-------------------|
| `Keycloak.AuthServices.Sdk` | 2.9.0 | `IKeycloakUserClient` for GetUsersAsync, GetUserAsync, UpdateUserAsync, DeleteUserAsync |
| `FluentValidation` | 12.1.1 | Command/DTO validation for update, block/unblock, delete |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1 | Paginated queries, search filters, audit log persistence |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0 | JWT validation + role-based `[Authorize(Roles = "admin")]` |
| `Serilog.AspNetCore` | 9.x | Structured audit logging |

### New Package Required
| Package | Version | License | Purpose |
|---------|---------|---------|---------|
| `Keycloak.AuthServices.Authorization` | 2.9.0 | MIT | `KeycloakRolesClaimsTransformation` — transforms nested `resource_access` roles into flat `ClaimTypes.Role` claims so `[Authorize(Roles = "admin")]` works out of the box. Already implied by Keycloak.AuthServices ecosystem. |

**Do NOT add**: Audit.NET (over-engineering — simple entity approach suffices), MediatR (commercial license — use manual DI CQRS), Dapper (EF Core handles all queries), MiniProfiler (OpenTelemetry already covers performance traces).

## Architecture Patterns

### Layered Structure

Follow the existing pattern established by `RegistrationController` → `RegisterClientCommandHandler` → `IClientRepository`:

```
Onboarding.API/Controllers/AdminUserController.cs       — HTTP layer
Onboarding.Application/Admin/Commands/                  — Command/Query types
Onboarding.Application/Admin/Validators/                — FluentValidation
Onboarding.Application/Common/ICommandHandler<T,R>      — Existing interface
Onboarding.Application/Common/IQueryHandler<T,R>        — Existing interface
Onboarding.Domain/Repositories/IAdminRepository.cs      — Domain abstractions
Onboarding.Domain/Aggregates/Audit/AuditLog.cs          — Audit entity
Onboarding.Infrastructure/Admin/AdminUserCommandHandler.cs — CQRS handlers
Onboarding.Infrastructure/Admin/AdminRepository.cs      — EF Core implementation
Onboarding.Infrastructure/Persistence/Configurations/   — EF mappings
```

### Controller Pattern

The `AdminUserController` follows the same structure as `RegistrationController` and `AuthController`:

- `[ApiController]`, `[Route("api/[controller]")]`
- Constructor injection of `ICommandHandler<T, R>` and `IQueryHandler<T, R>` (from `Onboarding.Application.Common`)
- FluentValidation via `IValidator<T>` injected per-action or constructor
- `[Authorize(Roles = "admin")]` on all endpoints
- Returns `ProblemDetails` / `ValidationProblemDetails` for errors
- Uses `[ProducesResponseType]` for Swagger documentation

### CQRS Commands and Queries

**Queries (read operations):**
- `GetPaginatedUsersQuery` → `PaginatedResult<UserSummaryDto>` (ADMIN-01)
- `GetUserDetailsQuery` → `UserDetailDto` (ADMIN-02)

**Commands (write operations):**
- `UpdateUserCommand` → `Unit` (ADMIN-03)
- `BlockUserCommand` → `Unit` (ADMIN-04)
- `UnblockUserCommand` → `Unit` (ADMIN-04)
- `DeleteUserCommand` → `Unit` (ADMIN-05)

Each command/query has its own handler implementing `ICommandHandler<T, R>` or `IQueryHandler<T, R>`, registered via `builder.Services.AddScoped<...>()` in `DependencyInjection.cs`.

### Role-Based Authorization with Keycloak

The existing JWT setup uses `MapInboundClaims = false` (Program.cs line 115) — this preserves Keycloak claim names. However, **Keycloak does NOT emit flat `role` claims by default**. Roles are nested inside `resource_access.{clientId}.roles` and `realm_access.roles`.

To make `[Authorize(Roles = "admin")]` work, you MUST add `KeycloakRolesClaimsTransformation`:

```csharp
// In Program.cs, after AddAuthentication:
builder.Services.AddKeycloakAuthorization(builder.Configuration);
```

This registers `KeycloakRolesClaimsTransformation` which reads `resource_access["onboarding-api"]["roles"]` and flattens them into `ClaimTypes.Role` claims. Without this, `[Authorize(Roles = "admin")]` always returns 403 because the role claim is never found.

**IMPORTANT**: The `admin` role must be assigned in Keycloak as a **client role** on the `onboarding-api` client (or as a realm role if you configure the transformation to include realm roles). The `KeycloakRolesClaimsTransformation` only maps resource_access roles by default, NOT realm roles.

### Dual-Source Data Query (app_db + Keycloak)

For ADMIN-01 (paginated list) and ADMIN-02 (user details), data comes from two sources:
1. **app_db** (PostgreSQL): Name, email, CPF/CNPJ, phone, type, razao_social
2. **Keycloak Admin API**: `enabled` (blocked/unblocked), `emailVerified`, Keycloak internal status

**Pattern**: Query app_db first (primary source for pagination/search), then batch-fetch Keycloak status for the returned users. Do NOT query Keycloak per-user in a loop — that's N+1 over HTTP.

For paginated list: after getting the page of clients from EF Core, collect their emails, call `GetUsersAsync` once with email filter (or call individually if Keycloak doesn't support batch email lookup — see Keycloak section below).

**Reality check**: `IKeycloakUserClient.GetUsersAsync` supports `GetUsersRequestParameters { Email = email, Exact = true }` but this is single-email lookup. For batch, you MUST call it per-user. Limit the page size to 20-50 to keep HTTP overhead manageable.

## Keycloak Admin API Integration

### IKeycloakUserClient Methods Needed

| Method | Purpose | ADMIN Requirement |
|--------|---------|-------------------|
| `GetUsersAsync(realm, parameters, ct)` | Search/list users with filters | ADMIN-01 (paginated list) |
| `GetUserAsync(realm, userId, ct)` | Get single user details | ADMIN-02 (user details) |
| `UpdateUserAsync(realm, userId, userRepresentation, ct)` | Update user (enable/disable) | ADMIN-03, ADMIN-04 |
| `DeleteUserAsync(realm, userId, ct)` | Delete user from Keycloak | ADMIN-05 (LGPD deletion) |

### Block/Unblock User

Keycloak uses the `Enabled` property on `UserRepresentation` to control account status:

```csharp
// Block user
var user = await _keycloakUserClient.GetUserAsync(_realm, keycloakUserId, ct);
user.Enabled = false;
await _keycloakUserClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);

// Unblock user
var user = await _keycloakUserClient.GetUserAsync(_realm, keycloakUserId, ct);
user.Enabled = true;
await _keycloakUserClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);
```

**IMPORTANT**: The `UpdateUserAsync` method requires the FULL `UserRepresentation` object, not just the changed fields. You MUST fetch the user first, modify the `Enabled` property, then send the complete representation back. Partial updates are NOT supported by Keycloak Admin API.

**Race condition risk**: Between Get and Update, another process could modify the user. Use optimistic concurrency if Keycloak supports it (it does not natively for this endpoint). Mitigation: the block/unblock operations should be idempotent — setting `Enabled = false` when already `false` is a no-op.

### Delete User

`DeleteUserAsync(realm, userId, ct)` permanently removes the user from Keycloak. This is irreversible.

**LGPD flow**:
1. Anonymize PII in app_db FIRST (see LGPD section below)
2. THEN delete from Keycloak
3. If Keycloak delete fails: log as critical error, retry with exponential backoff, manual intervention fallback

### Keycloak Unavailability

All Keycloak Admin API calls should be wrapped in try/catch with:
- Retry policy for transient failures (HTTP 5xx, timeout) — use Polly or HttpClient resiliency
- Circuit breaker pattern: if Keycloak is down, fail fast after N consecutive failures
- Clear error responses: return 503 Service Unavailable with generic message (SEC-08)

The existing `KeycloakUserService` already has a pattern for handling transient errors — follow the same approach used in `RegisterClientCommandHandler.IsTransientKeycloakError`.

### manage-users Role Scope

The confidential client `onboarding-api-admin` must have the `manage-users` **client role** assigned in Keycloak. This role grants:
- CRUD operations on users within the realm
- User role mapping management
- Password reset operations
- User search and listing

It does NOT grant:
- Realm configuration changes
- Client management
- Role definition changes

## Role-Based Authorization

### How Keycloak Includes Roles in JWT

Keycloak JWT structure (access token):
```json
{
  "sub": "user-uuid",
  "preferred_username": "admin@example.com",
  "email": "admin@example.com",
  "realm_access": {
    "roles": ["offline_access", "uma_authorization", "default-roles-onboarding"]
  },
  "resource_access": {
    "onboarding-api": {
      "roles": ["admin"]
    },
    "account": {
      "roles": ["manage-account", "view-profile"]
    }
  },
  "aud": ["account", "onboarding-api"],
  "iss": "http://localhost:8180/realms/onboarding"
}
```

**The `admin` role lives at `resource_access["onboarding-api"]["roles"]`** — NOT at the top level.

### Configuration Steps

1. **In Keycloak Admin Console**:
   - Go to Realm `onboarding` → Clients → `onboarding-api` (confidential client)
   - Create a client role named `admin`
   - Assign this role to the admin user via Users → admin user → Role Mappings → Client Roles → `onboarding-api` → `admin`

2. **In .NET Program.cs**:
   ```csharp
   // After AddAuthentication + AddJwtBearer:
   builder.Services.AddKeycloakAuthorization(builder.Configuration);
   ```

3. **On the controller**:
   ```csharp
   [Authorize(Roles = "admin")]
   [Route("api/[controller]")]
   public sealed class AdminUserController : ControllerBase { ... }
   ```

### 403 vs 401

- **401 Unauthorized**: User is not authenticated (no valid JWT, expired token, invalid signature). Handled by JWT bearer middleware automatically.
- **403 Forbidden**: User is authenticated but does not have the `admin` role. ASP.NET Core authorization middleware returns this automatically when `[Authorize(Roles = "admin")]` fails.

**Do NOT manually check roles in controller actions.** The middleware handles it. Manual checks are redundant and introduce bugs.

### JWT Claim Name for Roles

After `KeycloakRolesClaimsTransformation`, roles are emitted as `ClaimTypes.Role` (which is `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"`). The `[Authorize(Roles = "admin")]` attribute reads this claim type automatically.

To get the admin's identity for audit logging:
```csharp
var adminEmail = User.FindFirst("email")?.Value;        // From Keycloak JWT
var adminSub = User.FindFirst("sub")?.Value;             // Keycloak user ID
```

## LGPD-Compliant Deletion

### Anonymization Strategy

**DO NOT hard-delete.** LGPD requires you to prove that data was deleted upon request. Use a soft-delete pattern with PII scrubbing:

```csharp
public async Task AnonymizeAsync(Guid clientId, CancellationToken ct = default)
{
    var client = await _db.Clients.FindAsync([clientId], ct);
    if (client is null) return;

    client.Name = "Usuário Excluído";
    client.Email = Email.Create($"deleted-{clientId}@internal.local");
    client.Phone = PhoneNumber.Create("+0000000000");
    // Set CPF/CNPJ/RazaoSocial to null or masked
    // Keep: Id, Type, CreatedAt (for audit), DeletedAt
    // Set DeletedAt = UtcNow
}
```

**What to keep**:
- `Id` (GUID) — needed for audit trail references
- `Type` (PF/PJ) — anonymized statistics
- `CreatedAt` — historical record
- `DeletedAt` — LGPD compliance timestamp
- Anonymized placeholder values for required fields

**What to scrub**:
- `Name` → "Usuário Excluído"
- `Email` → `deleted-{id}@internal.local`
- `Phone` → "+0000000000"
- `Cpf` → NULL
- `Cnpj` → NULL
- `RazaoSocial` → NULL

### Database Schema Change

Add to the `clients` table via EF Core migration:
```csharp
builder.Property(c => c.DeletedAt)
    .HasColumnName("deleted_at")
    .IsRequired(false);
```

And in the `Client` entity:
```csharp
public DateTime? DeletedAt { get; private set; }

public void Anonymize()
{
    if (DeletedAt.HasValue) return; // Already deleted — idempotent
    DeletedAt = DateTime.UtcNow;
    Name = "Usuário Excluído";
    // ... scrub all PII fields
}
```

### Compensation Strategy

If Keycloak delete fails AFTER app_db anonymization:

1. **Log as critical** — this is a data inconsistency state
2. **Retry with exponential backoff** — up to 3 attempts
3. **If all retries fail**: mark client with `KeycloakDeletionPending = true` flag
4. **Background job or manual intervention**: retry Keycloak deletion later
5. **NEVER rollback the anonymization** — PII has already been scrubbed, and user expects their data to be gone

```csharp
// In DeleteUserCommandHandler:
// 1. Anonymize DB
await _repository.AnonymizeAsync(command.ClientId, ct);

// 2. Delete from Keycloak
try
{
    await _keycloakUserService.DeleteUserByEmailAsync(email, ct);
}
catch (Exception ex)
{
    _logger.LogCritical(ex, "Keycloak deletion failed for user {Email} after DB anonymization", email);
    // Set pending flag for manual retry
    await _repository.MarkKeycloakDeletionPendingAsync(command.ClientId, ct);
    // Still return success to user — their PII is gone from our DB
}
```

### Audit Log for Deletion

Every deletion MUST produce an audit log entry:
```json
{
  "auditId": "uuid",
  "adminSub": "admin-user-sub",
  "adminEmail": "admin@example.com",
  "action": "USER_DELETED",
  "targetUserId": "deleted-user-id",
  "targetEmail": "user@example.com",
  "timestamp": "2026-04-09T12:00:00Z",
  "snapshot": { "email": "user@example.com", "name": "John Doe", "type": "PF" }
}
```

The `snapshot` captures what the user looked like BEFORE anonymization — this is required for LGPD proof of what was deleted.

## Pagination + Search

### EF Core Pagination Pattern

**CRITICAL**: Always use `OrderBy` before `Skip` and `Take`. Without explicit ordering, PostgreSQL returns rows in arbitrary order, causing duplicate/missing items across pages.

```csharp
var query = _db.Clients
    .AsNoTracking()
    .Where(c => !c.DeletedAt.HasValue) // Exclude deleted users
    .OrderBy(c => c.Name)              // REQUIRED before Skip/Take
    .ThenBy(c => c.Id)                 // Tiebreaker for stable ordering
    .Skip((query.Page - 1) * query.PageSize)
    .Take(query.PageSize)
    .Select(c => new UserSummaryDto(...))
    .ToListAsync(ct);
```

**Do NOT use**: `.Skip(n).Take(m)` without `OrderBy` — this is undefined behavior and will return inconsistent results.

**Do NOT use**: `ToListAsync()` before pagination — fetches entire table into memory.

### Search Implementation

Search across name, CPF, CNPJ, email using case-insensitive, culture-invariant comparisons:

```csharp
IQueryable<Client> ApplySearch(IQueryable<Client> query, string? search)
{
    if (string.IsNullOrWhiteSpace(search)) return query;

    var normalized = search.Trim().ToLowerInvariant();

    // Remove formatting chars from search term for document matching
    var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

    return query.Where(c =>
        EF.Functions.ILike(c.Name, $"%{normalized}%") ||        // ILike = case-insensitive (PostgreSQL)
        EF.Functions.ILike(c.Email.Value, $"%{normalized}%") ||
        (c.Cpf != null && c.Cpf.Value.Contains(normalized)) ||
        (c.Cnpj != null && c.Cnpj.Value.Contains(normalized)) ||
        (digitsOnly.Length > 0 && (
            (c.Cpf != null && c.Cpf.Value.Contains(digitsOnly)) ||
            (c.Cnpj != null && c.Cnpj.Value.Contains(digitsOnly))
        ))
    );
}
```

**Why `EF.Functions.ILike`**: PostgreSQL `ILIKE` is case-insensitive and uses indexes. `.Contains()` translates to `LIKE '%...%'` which is case-sensitive and does a sequential scan.

**CPF/CNPJ normalization**: Store CPF as 11 digits (no dots/dashes), CNPJ as 14 digits. When searching, strip formatting from the search term so "123.456.789-00" matches "12345678900".

### Status Filter

Filter by client status (active, blocked, deleted):
```csharp
IQueryable<Client> ApplyStatusFilter(IQueryable<Client> query, string? status)
{
    return status?.ToLowerInvariant() switch
    {
        "active" => query.Where(c => !c.DeletedAt.HasValue),
        "blocked" => query // Blocked status comes from Keycloak — filter post-query
        "deleted" => query.Where(c => c.DeletedAt.HasValue),
        _ => query
    };
}
```

**Note**: "Blocked" status comes from Keycloak's `Enabled` property, not from app_db. For a clean filter, you need to cross-reference. Two approaches:
1. **Filter in memory** after fetching the page — simpler but less efficient
2. **Join with a local mirror table** that caches Keycloak enabled status — more complex but enables server-side filtering

For Phase 16, use approach 1 (in-memory filter) and note that blocked filtering applies to the current page only. Full server-side blocked filtering requires a cached Keycloak status column (future improvement).

### Paginated Response DTO

```csharp
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
```

## Audit Logging

### Audit Entity Design

Use a simple entity-based approach in app_db. Do NOT use Audit.NET (adds complexity, tight coupling, and is overkill for this scope).

```csharp
// Domain entity
public sealed class AuditLog : Entity<Guid>
{
    public string AdminSub { get; private set; } = default!;      // Keycloak sub of admin
    public string AdminEmail { get; private set; } = default!;    // Email of admin
    public string Action { get; private set; } = default!;        // e.g., "USER_BLOCKED"
    public Guid? TargetUserId { get; set; }                       // Target user ID (nullable for system actions)
    public string TargetEmail { get; set; } = default!;           // Target email for readability
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? SnapshotBefore { get; set; }                   // JSON snapshot of state before change
    public string? SnapshotAfter { get; set; }                    // JSON snapshot of state after change
    public string? IpAddress { get; set; }                        // Admin's IP
    public string? UserAgent { get; set; }                        // Admin's user agent
}
```

### Minimum Viable Schema

| Column | Type | Nullable | Purpose |
|--------|------|----------|---------|
| `id` | UUID | NOT NULL | Primary key |
| `admin_sub` | VARCHAR(255) | NOT NULL | Keycloak sub of admin user |
| `admin_email` | VARCHAR(320) | NOT NULL | Email of admin user |
| `action` | VARCHAR(50) | NOT NULL | Action type enum |
| `target_user_id` | UUID | NULL | Target user |
| `target_email` | VARCHAR(320) | NOT NULL | Target email (human-readable) |
| `timestamp` | TIMESTAMPTZ | NOT NULL | When it happened (UTC) |
| `snapshot_before` | JSONB | NULL | State before change |
| `snapshot_after` | JSONB | NULL | State after change |
| `ip_address` | VARCHAR(45) | NULL | Admin IP (IPv6 max) |
| `user_agent` | VARCHAR(500) | NULL | Admin user agent |

### Writing Audit Logs

**Write transactionally** within the same DB transaction as the main operation. Do NOT use fire-and-forget for admin actions — if the main operation commits, the audit log MUST commit too.

```csharp
public async Task LogAsync(AuditLog log, CancellationToken ct = default)
{
    await _db.AuditLogs.AddAsync(log, ct);
    // SaveChanges is called by the caller's transaction boundary
}
```

For operations that only affect Keycloak (block/unblock), write the audit log to app_db in a separate transaction after the Keycloak operation succeeds. If audit write fails, retry — never silently skip.

### Extracting Admin Identity from JWT

```csharp
private AuditContext ExtractAuditContext()
{
    var adminSub = User.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("Missing 'sub' claim in JWT.");
    var adminEmail = User.FindFirst("email")?.Value
        ?? throw new InvalidOperationException("Missing 'email' claim in JWT.");
    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
    var ua = Request.Headers["User-Agent"].ToString();

    return new AuditContext(adminSub, adminEmail, ip, ua);
}
```

### Action Enum

```csharp
public static class AuditActions
{
    public const string UserViewed = "USER_VIEWED";
    public const string UserDetailsViewed = "USER_DETAILS_VIEWED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserBlocked = "USER_BLOCKED";
    public const string UserUnblocked = "USER_UNBLOCKED";
    public const string UserDeleted = "USER_DELETED";
    public const string UserAnonymized = "USER_ANONYMIZED";
}
```

## Common Pitfalls

### 1. EF Core Pagination Without OrderBy
```csharp
// WRONG — undefined behavior, inconsistent results across pages
var page = await _db.Clients.Skip(10).Take(10).ToListAsync(ct);

// CORRECT — explicit, stable ordering
var page = await _db.Clients
    .OrderBy(c => c.Name)
    .ThenBy(c => c.Id)  // Tiebreaker for users with same name
    .Skip(10)
    .Take(10)
    .ToListAsync(ct);
```

### 2. Trusting Frontend for Role Checks
NEVER rely on frontend to gate admin UI. Always enforce `[Authorize(Roles = "admin")]` server-side. A malicious user can craft requests to admin endpoints regardless of UI visibility.

### 3. Exposing Keycloak Internal IDs to Frontend
Do NOT return Keycloak UUIDs in API responses. Use the app_db `Client.Id` (GUID) as the stable identifier. Keycloak IDs are implementation details that may change if users are recreated.

### 4. Mixing app_db Users with Keycloak Status Without Caching
Querying Keycloak on every list request adds latency and rate-limit risk. For the paginated list, fetch Keycloak `Enabled` status per-user after the page is retrieved. Accept the N HTTP calls for pages of 20-50 users. For large-scale deployments, cache Keycloak status in a local column with a TTL.

### 5. Case-Sensitive CPF/CNPJ Search
CPF and CNPJ are stored as digits-only in the database. If the user searches "123.456", strip formatting before matching. Use culture-invariant string operations — `ToLowerInvariant()` and `char.IsDigit()`.

### 6. Forgetting MapInboundClaims = false
Already set in Program.cs (line 115). If removed, `User.FindFirst("email")` returns null because .NET maps "email" to the XML namespace URI. Do NOT remove this setting.

### 7. Keycloak UpdateUserAsync Requires Full Representation
```csharp
// WRONG — partial update loses all unset fields
var partial = new UserRepresentation { Enabled = false };
await _keycloakUserClient.UpdateUserAsync(_realm, userId, partial, ct);
// This may set FirstName, LastName, etc. to null

// CORRECT — fetch, modify, send full representation
var user = await _keycloakUserClient.GetUserAsync(_realm, userId, ct);
user.Enabled = false;
await _keycloakUserClient.UpdateUserAsync(_realm, userId, user, ct);
```

### 8. Hard-Delete Users
Never `DELETE FROM clients WHERE id = ...`. This breaks referential integrity (audit logs reference the user), makes LGPD auditing impossible, and loses historical data. Always anonymize.

### 9. Not Handling Keycloak Unavailability
If Keycloak is down, block/unblock/delete operations will fail. Return 503 Service Unavailable with a generic message. Do NOT expose the raw Keycloak error. Log the failure with full context for investigation.

### 10. Race Conditions in Block/Unblock
If two admins try to block/unblock the same user simultaneously, the last write wins. This is acceptable for Phase 16 — the operations are idempotent. For stronger guarantees, add an `ETag` or `UpdatedAt` check.

## Code Examples

### Paginated Query with EF Core (Correct Pattern)

```csharp
public sealed record GetPaginatedUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null);

public sealed record UserSummaryDto(
    Guid Id,
    string Name,
    string Email,
    string? Document,     // CPF or CNPJ formatted
    string Type,           // "PF" or "PJ"
    bool Enabled,          // From Keycloak
    DateTime? DeletedAt);

public sealed class GetPaginatedUsersHandler
    : IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>>
{
    private readonly AppDbContext _db;
    private readonly IKeycloakUserClient _keycloakUserClient;
    private readonly string _realm;

    public async Task<PaginatedResult<UserSummaryDto>> HandleAsync(
        GetPaginatedUsersQuery query, CancellationToken ct = default)
    {
        var baseQuery = _db.Clients.AsNoTracking();

        // Exclude deleted if status != "deleted"
        if (query.Status?.ToLowerInvariant() != "deleted")
            baseQuery = baseQuery.Where(c => !c.DeletedAt.HasValue);

        // Show only deleted if status == "deleted"
        if (query.Status?.ToLowerInvariant() == "deleted")
            baseQuery = baseQuery.Where(c => c.DeletedAt.HasValue);

        // Apply search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var normalized = query.Search.Trim().ToLowerInvariant();
            var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

            baseQuery = baseQuery.Where(c =>
                EF.Functions.ILike(c.Name, $"%{normalized}%") ||
                EF.Functions.ILike(c.Email.Value, $"%{normalized}%") ||
                (c.Cpf != null && c.Cpf.Value.Contains(normalized)) ||
                (c.Cnpj != null && c.Cnpj.Value.Contains(normalized)) ||
                (digitsOnly.Length > 0 && c.Cpf != null && c.Cpf.Value.Contains(digitsOnly)) ||
                (digitsOnly.Length > 0 && c.Cnpj != null && c.Cnpj.Value.Contains(digitsOnly)));
        }

        // Total count BEFORE pagination
        var totalCount = await baseQuery.CountAsync(ct);

        // Paginated items
        var clients = await baseQuery
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        // Enrich with Keycloak Enabled status
        var results = new List<UserSummaryDto>();
        foreach (var client in clients)
        {
            var enabled = true; // Default assumption
            if (client.Email is not null)
            {
                var kcUsers = await _keycloakUserClient.GetUsersAsync(
                    _realm,
                    new GetUsersRequestParameters { Email = client.Email.Value, Exact = true },
                    ct);
                enabled = kcUsers.FirstOrDefault()?.Enabled ?? true;
            }

            results.Add(new UserSummaryDto(
                client.Id,
                client.Name,
                client.Email?.Value ?? string.Empty,
                client.Cpf?.Value ?? client.Cnpj?.Value,
                client.Type.ToString(),
                enabled,
                client.DeletedAt));
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        return new PaginatedResult<UserSummaryDto>(results, totalCount, query.Page, query.PageSize, totalPages);
    }
}
```

### Role-Based Authorization with Keycloak JWT

```csharp
// Program.cs additions (after existing AddAuthentication + AddJwtBearer):
builder.Services.AddKeycloakAuthorization(builder.Configuration);

// AdminUserController.cs:
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public sealed class AdminUserController : ControllerBase
{
    // All endpoints inherit admin requirement from class-level attribute
}
```

### Audit Log Entity + Write Pattern

```csharp
// Onboarding.Domain/Aggregates/Audit/AuditLog.cs
public sealed class AuditLog : Entity<Guid>
{
    public string AdminSub { get; private set; } = default!;
    public string AdminEmail { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public Guid? TargetUserId { get; set; }
    public string TargetEmail { get; set; } = default!;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? SnapshotBefore { get; set; }
    public string? SnapshotAfter { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public static AuditLog Create(
        string adminSub,
        string adminEmail,
        string action,
        Guid? targetUserId,
        string targetEmail,
        object? snapshotBefore = null,
        object? snapshotAfter = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminSub = adminSub,
            AdminEmail = adminEmail,
            Action = action,
            TargetUserId = targetUserId,
            TargetEmail = targetEmail,
            SnapshotBefore = snapshotBefore != null
                ? JsonSerializer.Serialize(snapshotBefore)
                : null,
            SnapshotAfter = snapshotAfter != null
                ? JsonSerializer.Serialize(snapshotAfter)
                : null,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
}
```

### LGPD Anonymization Handler

```csharp
// In Client entity:
public void Anonymize()
{
    if (DeletedAt.HasValue) return; // Already anonymized — idempotent

    // Snapshot before (for audit)
    var before = new
    {
        Name = Name,
        Email = Email.Value,
        Phone = Phone.Value,
        Cpf = Cpf?.Value,
        Cnpj = Cnpj?.Value,
        RazaoSocial = RazaoSocial
    };

    DeletedAt = DateTime.UtcNow;
    Name = "Usuario Excluido";
    Email = Email.Create($"deleted-{Id}@internal.local");
    Phone = PhoneNumber.Create("+0000000000");
    Cpf = null;
    Cnpj = null;
    RazaoSocial = null;
}
```

### Block/Unblock User via Keycloak Admin API

```csharp
public sealed class BlockUserCommand(Guid UserId);

public sealed class BlockUserCommandHandler
    : ICommandHandler<BlockUserCommand, Unit>
{
    private readonly IKeycloakUserClient _keycloakUserClient;
    private readonly IClientRepository _clientRepository;
    private readonly IAuditLogRepository _auditRepository;
    private readonly ILogger<BlockUserCommandHandler> _logger;
    private readonly string _realm;

    public async Task<Unit> HandleAsync(BlockUserCommand command, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(command.UserId, ct);
        if (client is null)
            throw new NotFoundException($"Client {command.UserId} not found.");

        // Find Keycloak user by email
        var kcUsers = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = client.Email.Value, Exact = true },
            ct);

        var kcUser = kcUsers.FirstOrDefault();
        if (kcUser is null)
            throw new NotFoundException($"Keycloak user not found for email {client.Email.Value}.");

        if (kcUser.Enabled == false)
            return Unit.Value; // Already blocked — idempotent

        // Update Keycloak
        kcUser.Enabled = false;
        await _keycloakUserClient.UpdateUserAsync(_realm, kcUser.Id!, kcUser, ct);

        // Audit log
        var auditLog = AuditLog.Create(
            adminSub: "...",       // Extract from JWT
            adminEmail: "...",     // Extract from JWT
            action: AuditActions.UserBlocked,
            targetUserId: client.Id,
            targetEmail: client.Email.Value,
            snapshotBefore: new { Enabled = true },
            snapshotAfter: new { Enabled = false });

        await _auditRepository.AddAsync(auditLog, ct);

        return Unit.Value;
    }
}
```

## Don't Hand-Roll

### Pagination
**Use manual EF Core with `OrderBy` + `Skip` + `Take` + `AsNoTracking()`.**
Do NOT add Ardalis.Specification for this scope — the queries are straightforward and the extra abstraction adds complexity without benefit. The pattern shown above (with proper `OrderBy` tiebreaker) is the standard approach.

### Audit Logging
**Use a simple `AuditLog` entity in app_db.** Do NOT use Audit.NET, Serilog.Sinks.PostgreSQL, or custom event sourcing. The requirements are: who, what, when, target, before/after snapshot. A single table with JSON columns for snapshots handles this cleanly. It's queryable, transactional, and LGPD-compliant.

### Role Mapping
**Use Keycloak realm/client roles + `KeycloakRolesClaimsTransformation`.** Do NOT build a custom role system, do NOT store roles in app_db, do NOT implement RBAC from scratch. Keycloak is the source of truth for roles. The `Keycloak.AuthServices.Authorization` package provides the transformation that maps `resource_access["onboarding-api"]["roles"]` into `ClaimTypes.Role` claims automatically.

### Keycloak Integration
**Use `Keycloak.AuthServices.Sdk` (already in use).** Do NOT hand-roll HTTP calls to the Keycloak Admin API. The SDK provides `IKeycloakUserClient` with typed methods and proper token management. For operations not covered by the SDK (e.g., reset-password endpoint), use the existing `HttpClient` pattern already established in `KeycloakUserService`.

### JWT Role Validation
**Use `[Authorize(Roles = "admin")]` with `AddKeycloakAuthorization()`.** Do NOT manually parse JWT tokens, do NOT validate roles in middleware, do NOT implement custom authorization handlers. The built-in ASP.NET Core authorization pipeline handles it correctly when `KeycloakRolesClaimsTransformation` is registered.

### Error Responses
**Use `ProblemDetails` / `ValidationProblemDetails`.** Do NOT create custom error response classes. ASP.NET Core's built-in `ProblemDetails` follows RFC 7807 and is already used consistently across all existing controllers.
