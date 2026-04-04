# Phase 5: Registration API - Research

**Researched:** 2026-04-04
**Domain:** ASP.NET Core Controllers + EF Core + Keycloak Admin API + FluentValidation + Idempotency
**Confidence:** HIGH (core patterns), MEDIUM (Keycloak SDK specifics from GitHub source inspection)

---

## Summary

Phase 5 is the first phase where all four layers converge: the domain model from Phase 3, the
observability wiring from Phase 4, the Keycloak service wired in Phase 1/2, and new Infrastructure
and API layer code. The work divides into four verticals: (1) EF Core Infrastructure layer from
scratch (DbContext, ClientRepository, migrations), (2) the Registration controller wired to the
existing command handler, (3) Keycloak Admin API integration via IKeycloakUserClient, and (4)
cross-cutting concerns: idempotency, duplicate detection, FluentValidation with 422 responses, and
generic error messages for SEC-08.

The biggest architectural risk is the two-phase commit problem: app_db and Keycloak are separate
stores with no distributed transaction support. The design decision (from STATE.md) is to persist
to app_db first, then call Keycloak. If Keycloak fails, the compensation strategy is to delete the
already-persisted client row. This is a simple synchronous compensating transaction — not full Saga
orchestration — and is appropriate for this monolith scope.

**Primary recommendation:** Build the Infrastructure layer first (DbContext + ClientRepository +
EF Core migrations), then wire the controller, then integrate Keycloak user creation with
compensation, then add the idempotency filter as a decorator layer.

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| REG-03 | Validação server-side de CPF (algoritmo módulo 11) | Cpf value object already implements mod-11; FluentValidation wraps the exception as a validation error |
| REG-04 | Validação server-side de CNPJ (check-digit + formato alfanumérico 2026) | Cnpj value object already implements ASCII-48 mod-11; same wrapping pattern |
| REG-05 | Detecção de duplicatas — CPF/CNPJ/email únicos antes de criar user | IClientRepository.ExistsByX methods + EF Core unique index as DB-level safety net |
| REG-06 | Criação de user no Keycloak via Admin API após persistência no app_db | IKeycloakUserClient.CreateUserAsync via Keycloak.AuthServices.Sdk 2.9.0 |
| REG-08 | Idempotência no endpoint de registro (chave de idempotência para evitar double-submit) | IAsyncActionFilter reading Idempotency-Key header + IDistributedCache (in-memory) |
| BACK-05 | Controllers ASP.NET Core (sem Minimal API) | [ApiController] ControllerBase; manual FluentValidation injection |
| SEC-08 | Erros genéricos em todas as respostas de autenticação (sem information leakage) | Generic 422 body for validation failures; 409 for duplicate (no "user exists" hint) |
</phase_requirements>

---

## Project Constraints (from CLAUDE.md)

- **No MediatR** — ICommandHandler<TCommand,TResult> via built-in DI only (already built in Phase 3)
- **No FluentAssertions** — Shouldly (MIT) only for test assertions
- **No Minimal API** — [ApiController] + ControllerBase + Routes
- **OSS-only NuGet packages** — Apache 2.0 / MIT only; verify before adding any package
- **Keycloak.AuthServices.Sdk** — already in stack; use IKeycloakUserClient, not raw HttpClient
- **Duende.AccessTokenManagement** — already in stack for service account token lifecycle
- **No ASP.NET Core Identity** — Keycloak owns user management
- **EF Core 10 + Npgsql** — Infrastructure layer; no Dapper
- **FluentValidation 12.x** — manual validation pattern in controllers; no auto-validation pipeline

---

## Standard Stack

### Core (Phase 5 additions)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore | 10.0.5 | ORM for Infrastructure layer | Project standard; EF Core 10 for .NET 10 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | PostgreSQL provider for EF Core | Project standard; Npgsql team's official provider |
| FluentValidation | 12.1.1 | Validator classes in Application layer | Project standard; no auto-validation pipeline; manual only |
| Keycloak.AuthServices.Sdk | 2.9.0 | IKeycloakUserClient for Admin API | Already in CLAUDE.md stack; typed HTTP client |
| Duende.AccessTokenManagement | 4.2.0 | Service account token lifecycle (Apache 2.0) | Already in CLAUDE.md stack; auto-acquires CC tokens |
| Microsoft.Extensions.Caching.Memory | (SDK) | IDistributedCache (in-memory) for idempotency | Built into .NET SDK; no extra package needed for dev |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Testcontainers.Keycloak | 4.11.0 | Spin up real Keycloak in integration tests | Integration tests validating Keycloak user creation end-to-end |
| Testcontainers.PostgreSql | 4.11.0 | Spin up real PostgreSQL in integration tests | Integration tests for EF Core migrations and repository |
| Microsoft.EntityFrameworkCore.Design | 10.0.5 | `dotnet ef migrations add` CLI tooling | Infrastructure project; PrivateAssets=all |

### What NOT to Add

| Package | Why Avoid |
|---------|-----------|
| FluentValidation.AspNetCore | Deprecated and removed auto-validation; manual validation is correct approach in FV 12 |
| Polly | Not needed for single retry; add in a later hardening phase if needed |
| Npgsql (standalone) | Npgsql.EntityFrameworkCore.PostgreSQL brings it transitively |
| Any message broker | Out of scope; compensation is synchronous delete |

**Installation for Infrastructure project:**
```bash
dotnet add src/Onboarding.Infrastructure package Microsoft.EntityFrameworkCore --version 10.0.5
dotnet add src/Onboarding.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.1
dotnet add src/Onboarding.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 10.0.5
dotnet add src/Onboarding.Infrastructure package Keycloak.AuthServices.Sdk --version 2.9.0
dotnet add src/Onboarding.Infrastructure package Duende.AccessTokenManagement --version 4.2.0
```

**Installation for Application project (validators):**
```bash
dotnet add src/Onboarding.Application package FluentValidation --version 12.1.1
```

**Version verification (run before writing Standard Stack into any plan):**
```bash
dotnet package search "Microsoft.EntityFrameworkCore" --take 1
dotnet package search "Npgsql.EntityFrameworkCore.PostgreSQL" --take 1
dotnet package search "Keycloak.AuthServices.Sdk" --take 1
dotnet package search "Duende.AccessTokenManagement" --take 1
dotnet package search "FluentValidation" --take 1
```

---

## Architecture Patterns

### Recommended Project Structure

```
src/
├── Onboarding.Domain/
│   ├── Aggregates/ClientAggregate/Client.cs      (exists — sealed, protected ctor for EF)
│   ├── ValueObjects/Cpf.cs, Cnpj.cs, Email.cs    (exists)
│   └── Repositories/IClientRepository.cs         (exists)
│
├── Onboarding.Application/
│   ├── Clients/Commands/
│   │   ├── RegisterClientCommand.cs              (exists — Password field has TODO Phase 5)
│   │   └── RegisterClientCommandHandler.cs       (exists — needs IKeycloakUserService injected)
│   ├── Clients/Validators/
│   │   └── RegisterClientCommandValidator.cs     (NEW — FluentValidation 12 AbstractValidator)
│   ├── Clients/DTOs/
│   │   └── RegisterClientResult.cs               (exists)
│   └── Common/
│       ├── ICommandHandler.cs                    (exists)
│       └── IKeycloakUserService.cs               (NEW — application abstraction over Admin API)
│
├── Onboarding.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs                       (NEW — DbContext with value object mapping)
│   │   └── Configurations/ClientConfiguration.cs (NEW — IEntityTypeConfiguration<Client>)
│   ├── Repositories/
│   │   └── ClientRepository.cs                   (NEW — implements IClientRepository)
│   ├── Keycloak/
│   │   └── KeycloakUserService.cs                (NEW — implements IKeycloakUserService)
│   └── DependencyInjection.cs                    (NEW — AddInfrastructure() extension)
│
└── Onboarding.API/
    ├── Controllers/
    │   └── RegistrationController.cs             (NEW — POST /api/registration)
    ├── Filters/
    │   └── IdempotencyFilter.cs                  (NEW — IAsyncActionFilter reading Idempotency-Key)
    └── Program.cs                                (UPDATE — wire AddInfrastructure, AddApplication)

tests/
├── Onboarding.Domain.Tests/                      (exists — 38 tests green)
├── Onboarding.API.Tests/
│   ├── Registration/
│   │   ├── RegistrationControllerTests.cs        (NEW — integration tests, WebApplicationFactory)
│   │   └── IdempotencyFilterTests.cs             (NEW — filter unit tests)
│   └── (existing Observability/HealthChecks tests)
└── Onboarding.Integration.Tests/                 (NEW — Testcontainers for PG + Keycloak)
    └── Registration/
        └── RegistrationIntegrationTests.cs       (NEW — end-to-end with real DB + Keycloak)
```

### Pattern 1: IKeycloakUserService Application Abstraction

**What:** Create `IKeycloakUserService` in Application layer so the command handler depends on an
abstraction, not the Keycloak SDK directly. Infrastructure implements it with `IKeycloakUserClient`.

**Why:** Keeps Application layer free of infrastructure packages. Allows mocking in unit tests.

**Interface (Application layer):**
```csharp
// src/Onboarding.Application/Common/IKeycloakUserService.cs
namespace Onboarding.Application.Common;

public interface IKeycloakUserService
{
    Task<string> CreateUserAsync(
        string username,
        string email,
        string password,
        string firstName,
        CancellationToken ct = default);

    Task DeleteUserByEmailAsync(string email, CancellationToken ct = default);
}
```

**Implementation (Infrastructure layer):**
```csharp
// src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Onboarding.Application.Common;

public sealed class KeycloakUserService : IKeycloakUserService
{
    private readonly IKeycloakClient _keycloakClient;
    private readonly string _realm;

    public KeycloakUserService(IKeycloakClient keycloakClient, IConfiguration configuration)
    {
        _keycloakClient = keycloakClient;
        _realm = configuration["Keycloak:Realm"] ?? "onboarding";
    }

    public async Task<string> CreateUserAsync(
        string username, string email, string password, string firstName,
        CancellationToken ct = default)
    {
        var user = new UserRepresentation
        {
            Username = username,
            Email = email,
            FirstName = firstName,
            Enabled = true,
            EmailVerified = true,
            Credentials = new[]
            {
                new CredentialRepresentation
                {
                    Type = "password",
                    Value = password,
                    Temporary = false
                }
            }
        };
        await _keycloakClient.CreateUserAsync(_realm, user, ct);
        // Keycloak returns 201 with Location header; fetch user ID from GetUsers
        var users = await _keycloakClient.GetUsersAsync(_realm,
            new GetUsersRequestParameters { Email = email }, ct);
        return users.First().Id!;
    }

    public async Task DeleteUserByEmailAsync(string email, CancellationToken ct = default)
    {
        var users = await _keycloakClient.GetUsersAsync(_realm,
            new GetUsersRequestParameters { Email = email }, ct);
        var userId = users.FirstOrDefault()?.Id;
        if (userId is not null)
            await _keycloakClient.DeleteUserAsync(_realm, userId, ct);
    }
}
```

**Confidence note:** IKeycloakUserClient method signatures verified from GitHub source inspection
(MEDIUM confidence). `CreateUserAsync` returns Task (no body); the created user ID must be fetched
via `GetUsersAsync` by email as Keycloak does not return it in the response body. This is the
standard pattern for the Keycloak Admin API (HTTP 201 Location header parsing is also possible but
more complex).

### Pattern 2: Command Handler Extension for Keycloak Integration

The existing `RegisterClientCommandHandler` has a `// TODO Phase 5` comment. Phase 5 replaces the
stub with real Keycloak integration. The handler must be updated to:

1. Check duplicates first (fail fast, before DB write)
2. Persist to app_db
3. Call Keycloak
4. If Keycloak fails → delete from app_db (compensation)

```csharp
// Updated RegisterClientCommandHandler
public sealed class RegisterClientCommandHandler
    : ICommandHandler<RegisterClientCommand, Guid>
{
    private readonly IClientRepository _repository;
    private readonly IKeycloakUserService _keycloakUserService;

    public RegisterClientCommandHandler(
        IClientRepository repository,
        IKeycloakUserService keycloakUserService)
    {
        _repository = repository;
        _keycloakUserService = keycloakUserService;
    }

    public async Task<Guid> HandleAsync(
        RegisterClientCommand command, CancellationToken ct = default)
    {
        // 1. Duplicate detection (REG-05) — fail fast before DB write
        if (!string.IsNullOrEmpty(command.Cpf) &&
            await _repository.ExistsByCpfAsync(
                command.Cpf.Replace(".", "").Replace("-", ""), ct))
            throw new DuplicateClientException("CPF already registered");

        if (!string.IsNullOrEmpty(command.Cnpj) &&
            await _repository.ExistsByCnpjAsync(
                command.Cnpj.Replace(".", "").Replace("/", "").Replace("-", ""), ct))
            throw new DuplicateClientException("CNPJ already registered");

        if (await _repository.ExistsByEmailAsync(command.Email.ToLowerInvariant(), ct))
            throw new DuplicateClientException("Email already registered");

        // 2. Build domain aggregate (value objects validate here)
        var client = command.Cpf is not null
            ? Client.RegisterPessoaFisica(
                command.Nome, command.Cpf, command.Email, command.Phone)
            : Client.RegisterPessoaJuridica(
                command.RazaoSocial!, command.Cnpj!, command.Email, command.Phone);

        // 3. Persist to app_db FIRST (per architectural decision in STATE.md)
        await _repository.AddAsync(client, ct);

        // 4. Create Keycloak user — compensate on failure (REG-06)
        try
        {
            await _keycloakUserService.CreateUserAsync(
                username: command.Email,
                email: command.Email,
                password: command.Password,
                firstName: command.Nome,
                ct: ct);
        }
        catch (Exception ex) when (IsKeycloakError(ex))
        {
            // Compensation: remove the persisted row — Keycloak is source of truth for auth
            await _repository.DeleteAsync(client.Id, ct);
            throw new RegistrationFailedException(
                "User registration failed. Please try again.", ex);
        }

        return client.Id;
    }

    private static bool IsKeycloakError(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or InvalidOperationException;
}
```

**CRITICAL:** `IClientRepository` needs a `DeleteAsync(Guid id, CancellationToken ct)` method added
for the compensation step. This is a new method not yet in the interface.

### Pattern 3: EF Core Value Object Configuration

The `Client` aggregate uses owned value objects (Cpf, Cnpj, Email, PhoneNumber). EF Core cannot map
`sealed record` value types directly to columns — they must be configured explicitly via
`OwnsOne` or by mapping the `Value` property column.

**Recommended:** Map value object columns directly to avoid the `OwnsOne` owned entity nesting:

```csharp
// src/Onboarding.Infrastructure/Persistence/Configurations/ClientConfiguration.cs
public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        // Value object mapping — store only the normalized Value string
        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasConversion(
                vo => vo.Value,
                s => Email.Create(s))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasColumnName("phone")
            .HasConversion(
                vo => vo.Value,
                s => PhoneNumber.Create(s))
            .HasMaxLength(15);

        // Nullable value objects
        builder.Property(c => c.Cpf)
            .HasColumnName("cpf")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null : Cpf.Create(s))
            .HasMaxLength(11);

        builder.Property(c => c.Cnpj)
            .HasColumnName("cnpj")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null : Cnpj.Create(s))
            .HasMaxLength(14);

        builder.Property(c => c.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(200);

        // Unique indexes — DB-level safety net for REG-05
        // PostgreSQL NULLs are treated as non-equal in unique indexes by default,
        // so partial unique indexes on nullable columns are correct without HasFilter.
        builder.HasIndex(c => c.Email).IsUnique();

        builder.HasIndex(c => c.Cpf)
            .IsUnique()
            .HasFilter("cpf IS NOT NULL");   // Only enforce uniqueness on non-null rows

        builder.HasIndex(c => c.Cnpj)
            .IsUnique()
            .HasFilter("cnpj IS NOT NULL");

        // Idempotency key store (for REG-08)
        // Stored separately — see IdempotencyRecord entity below
    }
}
```

**IMPORTANT CAVEAT:** EF Core's `HasConversion` with a value object `Create` factory method that
throws exceptions on invalid input can cause problems during EF Core materialization (reading from
DB). The stored values in the DB are always already-valid normalized strings, so this is safe —
`Create` will not throw on rows written by the same application. Add a comment documenting this.

### Pattern 4: FluentValidation 12 Manual Validation in Controller

The `FluentValidation.AspNetCore` auto-pipeline package is deprecated in FV 12. The correct
pattern is manual injection of `IValidator<T>` and explicit `ValidateAsync` call.

```csharp
// src/Onboarding.Application/Clients/Validators/RegisterClientCommandValidator.cs
using FluentValidation;
using Onboarding.Application.Clients.Commands;

public sealed class RegisterClientCommandValidator
    : AbstractValidator<RegisterClientCommand>
{
    public RegisterClientCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        // PF path: exactly one of Cpf/Cnpj must be set
        When(x => x.Cpf is not null, () =>
        {
            RuleFor(x => x.Cpf!)
                .Must(cpf => IsValidCpfFormat(cpf))
                .WithMessage("Invalid CPF format.");
            // NOTE: deep CPF check-digit validation happens inside Cpf.Create()
            // in the domain layer — FluentValidation here only checks structural format.
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome is required for Pessoa Física.");
        });

        When(x => x.Cnpj is not null, () =>
        {
            RuleFor(x => x.Cnpj!)
                .Must(cnpj => IsValidCnpjFormat(cnpj))
                .WithMessage("Invalid CNPJ format.");
            RuleFor(x => x.RazaoSocial)
                .NotEmpty().WithMessage("Razão Social is required for Pessoa Jurídica.");
        });

        // Must provide exactly one document type
        RuleFor(x => x)
            .Must(x => (x.Cpf is not null) != (x.Cnpj is not null))
            .WithMessage("Provide either CPF (PF) or CNPJ (PJ), not both.")
            .OverridePropertyName("DocumentType");
    }

    private static bool IsValidCpfFormat(string? cpf) =>
        cpf is not null && System.Text.RegularExpressions.Regex.IsMatch(
            cpf.Replace(".", "").Replace("-", ""), @"^\d{11}$");

    private static bool IsValidCnpjFormat(string? cnpj) =>
        cnpj is not null && System.Text.RegularExpressions.Regex.IsMatch(
            cnpj.Replace(".", "").Replace("/", "").Replace("-", ""), @"^[A-Z0-9]{14}$");
}
```

**Controller pattern:**
```csharp
// src/Onboarding.API/Controllers/RegistrationController.cs
[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationController : ControllerBase
{
    private readonly ICommandHandler<RegisterClientCommand, Guid> _handler;
    private readonly IValidator<RegisterClientCommand> _validator;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        ICommandHandler<RegisterClientCommand, Guid> handler,
        IValidator<RegisterClientCommand> validator,
        ILogger<RegistrationController> logger)
    {
        _handler = handler;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterClientRequest request,
        CancellationToken ct)
    {
        // Map request DTO → command (keeps HTTP concerns out of Application layer)
        var command = new RegisterClientCommand(
            request.Nome,
            request.Cpf, request.Cnpj, request.RazaoSocial,
            request.Email, request.Phone, request.Password);

        // FluentValidation — structural validation (format, required fields)
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            // SEC-08: validation error body must NOT reveal user existence
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ));
        }

        try
        {
            var clientId = await _handler.HandleAsync(command, ct);
            return Created($"/api/clients/{clientId}", new { id = clientId });
        }
        catch (ArgumentException ex)
        {
            // Domain validation failure (bad check digit etc.) — REG-03, REG-04
            // SEC-08: generic message, never leak "user exists" information
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = "The provided document number is invalid."
                // ex.Message intentionally NOT propagated to response (could leak info)
            });
        }
        catch (DuplicateClientException)
        {
            // REG-05 + SEC-08: 409 Conflict but with generic message — no hint about which field
            return Conflict(new ProblemDetails
            {
                Title = "Registration conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "A client with the provided information already exists."
            });
        }
        catch (RegistrationFailedException)
        {
            // Keycloak failure after DB persist — compensation already ran in handler
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Registration temporarily unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Please try again in a few moments."
            });
        }
    }
}
```

### Pattern 5: Idempotency Filter (REG-08)

Use an `IAsyncActionFilter` attribute that reads the `Idempotency-Key` header, checks
`IDistributedCache`, and either returns the cached 201 response or executes and caches it.

```csharp
// src/Onboarding.API/Filters/IdempotencyFilter.cs
[AttributeUsage(AttributeTargets.Method)]
internal sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const int CacheMinutes = 60;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(
                "Idempotency-Key", out var keyValue) ||
            !Guid.TryParse(keyValue, out var idempotencyKey))
        {
            // No key supplied — treat as non-idempotent (allow through)
            // Per REG-08: key is OPTIONAL. Without it, double-submit is client's problem.
            await next();
            return;
        }

        var cache = context.HttpContext.RequestServices
            .GetRequiredService<IDistributedCache>();

        string cacheKey = $"idem:{idempotencyKey}";
        var cached = await cache.GetStringAsync(cacheKey);

        if (cached is not null)
        {
            var stored = JsonSerializer.Deserialize<IdempotentResponse>(cached)!;
            context.Result = new ObjectResult(stored.Value) { StatusCode = stored.StatusCode };
            return;
        }

        var executed = await next();

        // Cache only 2xx responses
        if (executed.Result is ObjectResult { StatusCode: >= 200 and < 300 } result)
        {
            var response = new IdempotentResponse(result.StatusCode ?? 200, result.Value);
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes)
                });
        }
    }
}
```

**DI registration in Program.cs:**
```csharp
// Add distributed memory cache for idempotency (dev only — replace with Redis in prod)
builder.Services.AddDistributedMemoryCache();
```

Apply to the controller action:
```csharp
[HttpPost]
[Idempotent]
public async Task<IActionResult> Register(...)
```

### Pattern 6: Keycloak Admin HTTP Client Wiring

```csharp
// In DependencyInjection.cs (Infrastructure) or Program.cs
var keycloakOptions = new KeycloakAdminClientOptions
{
    AuthServerUrl = configuration["Keycloak:Authority"]!
        .Replace("/realms/onboarding", "/"),  // base URL without realm
    Realm = "onboarding",
    Resource = configuration["Keycloak:AdminClientId"]!,
};

// Token management — service account CC grant
services.AddDistributedMemoryCache();
services.AddClientCredentialsTokenManagement()
    .AddClient("keycloak-admin", client =>
    {
        client.ClientId = keycloakOptions.Resource;
        client.ClientSecret = configuration["Keycloak:AdminClientSecret"];
        client.TokenEndpoint = new Uri(
            $"{keycloakOptions.AuthServerUrl}realms/{keycloakOptions.Realm}" +
            "/protocol/openid-connect/token");
    });

services.AddKeycloakAdminHttpClient(keycloakOptions)
    .AddClientCredentialsTokenHandler("keycloak-admin");

services.AddScoped<IKeycloakUserService, KeycloakUserService>();
```

### Anti-Patterns to Avoid

- **Don't check duplicates via DB unique constraint exception only:** The 23505 PostgresException
  from Npgsql can be caught as `DbUpdateException.InnerException`, but it requires parsing
  `ConstraintName` to know which field caused the violation. Pre-check with
  `ExistsByEmailAsync`/`ExistsByCpfAsync`/`ExistsByCnpjAsync` before writing is cleaner, more
  readable, and required for the SEC-08 generic error message to be accurate. Keep the DB unique
  index as a safety net only.

- **Don't leak exception messages to HTTP responses:** `ArgumentException.Message` from domain
  value objects contains useful developer info ("CPF check digit invalid") but MUST NOT appear in
  the HTTP response body. SEC-08 requires generic messages.

- **Don't use `FluentValidation.AspNetCore` auto-pipeline:** The package is no longer maintained.
  Use manual `IValidator<T>` injection.

- **Don't call `SaveChangesAsync` inside `AddAsync`:** The repository's `AddAsync` should call
  `_context.Clients.Add(client)` and `_context.SaveChangesAsync()` to complete the unit of work.
  Alternatively, the controller/handler manages the unit of work explicitly. Recommended: repository
  handles save for simplicity at this scope.

- **Don't map value objects with `OwnsOne` in this codebase:** `OwnsOne` creates a shadow table
  join which complicates unique index definitions. Use `HasConversion` to map the `Value` string
  directly to a column.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Service account token lifecycle | Manual HttpClient + token store | Duende.AccessTokenManagement 4.x | Handles expiry, caching, retry automatically |
| Keycloak user creation HTTP | Raw HttpClient with JSON serialization | Keycloak.AuthServices.Sdk IKeycloakUserClient | Typed client, handles realm routing, Content-Type headers |
| Idempotency key storage | Custom Redis or DB table | IDistributedCache (in-memory → Redis) | Standard .NET abstraction; swap backing store without code change |
| EF Core value conversion | Custom IValueConverter from scratch | `HasConversion` with lambda pair | Built-in EF Core feature since 2.1 |
| 422 response shape | Custom JSON response builder | `ValidationProblemDetails` (built-in) | RFC 9457 compatible; standard shape |

---

## Rollback / Compensation Strategy

**Decision from STATE.md:** app_db persists first, then Keycloak is called.

**Failure modes:**

| Failure Point | What Happened | Compensation |
|---------------|--------------|--------------|
| FluentValidation fails | Nothing written anywhere | Return 422; no compensation needed |
| Value object throws (bad check digit) | Nothing written anywhere | Return 422; no compensation needed |
| Duplicate detected by ExistsByX check | Nothing written anywhere | Return 409; no compensation needed |
| `_repository.AddAsync` throws | DB constraint or connection failure | Re-throw; Keycloak never called |
| `_keycloakUserService.CreateUserAsync` throws | Row in app_db, no Keycloak user | Call `_repository.DeleteAsync(client.Id)` and re-throw as 503 |
| `_repository.DeleteAsync` in compensation throws | Orphaned row in app_db | Log as Critical; return 503; requires manual cleanup or a reconciliation job |

**IClientRepository must add `DeleteAsync`:**
```csharp
Task DeleteAsync(Guid id, CancellationToken ct = default);
```

**This is a BREAKING CHANGE to IClientRepository** — tests that mock the interface must be updated.

**Orphaned rows:** The risk of a permanently orphaned app_db row (if compensation itself fails) is
documented and accepted for v1. A future reconciliation job can detect rows with no corresponding
Keycloak user. Log the failure with Serilog at `LogEventLevel.Critical` with the `ClientId` so it
is findable.

---

## SEC-08: Generic Error Messages

All HTTP error responses from the registration endpoint must NOT reveal user existence:

| Scenario | Bad Response (leaks info) | Correct Response (SEC-08) |
|----------|--------------------------|--------------------------|
| CPF already registered | "CPF 529.982.247-25 is already in use" | "A client with the provided information already exists." |
| Email already registered | "Email user@domain.com is already taken" | "A client with the provided information already exists." |
| Bad CPF check digit | "CPF check digit invalid" | "The provided document number is invalid." |
| Keycloak user already exists (409 from Admin API) | "User with email X already exists in Keycloak" | Same 409 as duplicate detection above |

**Implementation rule:** In catch blocks, NEVER propagate `ex.Message` to the response body.
Use static string literals for all user-facing error messages.

---

## Common Pitfalls

### Pitfall 1: EF Core value object conversion throws during database reads

**What goes wrong:** `HasConversion` calls `Cpf.Create(s)` on materialization. If somehow invalid
data is stored in the DB (e.g., from manual SQL edits), EF throws during SELECT.

**Why it happens:** The converter is invoked for every row read from the database.

**How to avoid:** Store only already-validated normalized strings. The DB unique constraint + the
`Create` factory guarantee that only valid values reach the column. Document this assumption.

**Warning signs:** `ArgumentException` appearing during GET requests (not POST).

### Pitfall 2: Keycloak CreateUserAsync returns 409 for duplicate email

**What goes wrong:** Keycloak returns HTTP 409 if the username (email) already exists. The
`IKeycloakUserClient.CreateUserAsync` throws an `HttpRequestException` wrapping this 409.

**Why it happens:** Keycloak deduplicates on username globally within the realm.

**How to avoid:** The pre-check via `ExistsByEmailAsync` in the handler catches this case before
calling Keycloak. However, there is a race condition window. Catch the Keycloak 409 specifically
and re-throw as `DuplicateClientException` to return 409 from the controller.

**Warning signs:** `HttpRequestException` with status 409 in compensation path.

### Pitfall 3: Idempotency filter caches 4xx responses

**What goes wrong:** If you cache all responses (not just 2xx), a transient validation error
gets permanently cached. The client can never succeed with the same idempotency key.

**Why it happens:** Simple "cache everything" implementation.

**How to avoid:** Only cache `StatusCode >= 200 && < 300` as shown in the filter pattern above.
Failed requests return the error without caching, allowing retry with the same key.

### Pitfall 4: Nullable value object column index

**What goes wrong:** EF Core on PostgreSQL creates a unique index on a nullable column. If two PJ
clients exist with `Cpf = null`, PostgreSQL will raise a unique violation because null != null is
FALSE in PostgreSQL's unique index (unlike SQL Server).

**Why it happens:** PostgreSQL treats `NULL = NULL` as `true` for unique index purposes (partial
indexes needed).

**How to avoid:** Use `HasFilter("cpf IS NOT NULL")` for the CPF index (PF only) and
`HasFilter("cnpj IS NOT NULL")` for CNPJ. This creates a partial unique index that ignores nulls.
Email is always non-null so no filter needed there.

### Pitfall 5: Keycloak Admin API requires realm-level auth, not user-level

**What goes wrong:** Using the wrong token — the user's bearer token instead of the service
account client credentials token — for admin operations. The service account `onboarding-api-admin`
with `manage-users` role is required.

**Why it happens:** Confusion about which token goes to which endpoint.

**How to avoid:** The `AddClientCredentialsTokenHandler` from Duende attaches the CC token
automatically to the `AddKeycloakAdminHttpClient` HTTP client. Keep these two registrations together
in `DependencyInjection.cs` and never inject `IHttpClientFactory` directly for admin calls.

### Pitfall 6: EF Core migration applied in wrong context

**What goes wrong:** Running `dotnet ef migrations add` from wrong directory or without
`--project`/`--startup-project` flags.

**Why it happens:** EF Core design-time tooling needs both the DbContext project and a startup
project that has the full DI wiring.

**How to avoid:**
```bash
dotnet ef migrations add InitialCreate \
  --project src/Onboarding.Infrastructure \
  --startup-project src/Onboarding.API \
  --output-dir Persistence/Migrations
```

---

## Code Examples

### Create User in Keycloak (verified pattern from SDK source inspection)

```csharp
// Source: https://github.com/NikiforovAll/keycloak-authorization-services-dotnet
// IKeycloakUserClient.CreateUserAsync signature:
// Task CreateUserAsync(string realm, UserRepresentation user, CancellationToken ct = default)

var user = new UserRepresentation
{
    Username = email,          // Username = email is the convention for this app
    Email = email,
    FirstName = name,
    Enabled = true,
    EmailVerified = true,
    Credentials = new[]
    {
        new CredentialRepresentation
        {
            Type = "password",
            Value = password,
            Temporary = false
        }
    }
};
await _keycloakClient.CreateUserAsync("onboarding", user, ct);
```

### EF Core unique index with null filter (verified from MS docs)

```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/modeling/indexes
// Partial unique index for nullable columns in PostgreSQL

builder.HasIndex(c => c.Cpf)
    .IsUnique()
    .HasFilter("cpf IS NOT NULL");
```

### FluentValidation DI registration (verified from FV docs)

```csharp
// Scans the assembly for all AbstractValidator<T> implementations
// Source: https://docs.fluentvalidation.net/en/latest/di.html
builder.Services.AddValidatorsFromAssemblyContaining<RegisterClientCommandValidator>();
```

### Catch PostgresException 23505 (as DB safety net, not primary path)

```csharp
// Source: Npgsql documentation — SqlState codes
// Used as fallback if concurrent duplicate slips past ExistsByX pre-check
catch (DbUpdateException ex)
    when (ex.InnerException is PostgresException { SqlState: "23505" })
{
    // DB-level unique constraint violation — treat as duplicate
    throw new DuplicateClientException(
        "Concurrent registration conflict detected.", ex);
}
```

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Docker | Testcontainers integration tests | Yes | 29.2.1 | Skip integration tests in CI without Docker |
| .NET 10 SDK | All build/test | Yes | 10.0.201 | None — required |
| PostgreSQL (via Docker) | EF Core migrations, integration tests | Via Docker | postgres:16-alpine | — |
| Keycloak (via Docker) | Keycloak integration tests | Via Docker | quay.io/keycloak:26.1 | Mock IKeycloakUserService in unit tests |

**Notes:**
- For unit tests and controller tests: mock `IKeycloakUserService` and `IClientRepository` with NSubstitute — no containers needed
- For integration tests: Testcontainers.PostgreSql + Testcontainers.Keycloak spin real instances

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Shouldly 4.3.0 + NSubstitute 5.3.0 |
| Existing test project (unit + API) | `tests/Onboarding.API.Tests/` |
| Existing test project (domain) | `tests/Onboarding.Domain.Tests/` |
| New test project (integration) | `tests/Onboarding.Integration.Tests/` (Testcontainers) |
| Quick run command | `dotnet test tests/Onboarding.Domain.Tests/ tests/Onboarding.API.Tests/` |
| Full suite command | `dotnet test` (all projects) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| REG-03 | CPF check digit rejection returns 422 | Integration (WebApplicationFactory) | `dotnet test tests/Onboarding.API.Tests/ --filter "Registration"` | No — Wave 0 |
| REG-03 | Valid CPF accepted, client persisted | Integration (WebApplicationFactory) | same | No — Wave 0 |
| REG-04 | Invalid CNPJ returns 422 | Integration (WebApplicationFactory) | same | No — Wave 0 |
| REG-04 | Alphanumeric CNPJ accepted | Integration (WebApplicationFactory) | same | No — Wave 0 |
| REG-05 | Duplicate CPF returns 409 | Integration (WebApplicationFactory) | same | No — Wave 0 |
| REG-05 | Duplicate CNPJ returns 409 | Integration (WebApplicationFactory) | same | No — Wave 0 |
| REG-05 | Duplicate email returns 409 | Integration (WebApplicationFactory) | same | No — Wave 0 |
| REG-06 | Valid PF POST creates Keycloak user | Integration (Testcontainers) | `dotnet test tests/Onboarding.Integration.Tests/` | No — Wave 0 |
| REG-06 | Keycloak failure triggers compensation delete | Unit (NSubstitute) | `dotnet test tests/Onboarding.Domain.Tests/` | No — Wave 0 |
| REG-08 | Same Idempotency-Key → single record, 201 cached | Integration (WebApplicationFactory) | same as REG-03 | No — Wave 0 |
| REG-08 | Missing Idempotency-Key → request proceeds | Integration (WebApplicationFactory) | same | No — Wave 0 |
| BACK-05 | Controller exists, route /api/registration responds | Integration (WebApplicationFactory) | same | No — Wave 0 |
| SEC-08 | 409 response body has no "email exists" text | Integration (WebApplicationFactory) | same | No — Wave 0 |
| SEC-08 | 422 response body has no "user exists" text | Integration (WebApplicationFactory) | same | No — Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test tests/Onboarding.Domain.Tests/ tests/Onboarding.API.Tests/`
- **Per wave merge:** `dotnet test` (all test projects, including Onboarding.Integration.Tests)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

Unit test gaps (in Onboarding.Domain.Tests):
- [ ] `tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs` — add tests for Keycloak failure compensation path and duplicate detection (extend existing file)

API integration test gaps (in Onboarding.API.Tests):
- [ ] `tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs` — REG-03, REG-04, REG-05, REG-08, BACK-05, SEC-08 behaviors via WebApplicationFactory (mock IKeycloakUserService + IClientRepository)
- [ ] `tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs` — filter unit tests

New integration test project (requires Docker):
- [ ] `tests/Onboarding.Integration.Tests/Onboarding.Integration.Tests.csproj` — new xUnit project with Testcontainers.Keycloak + Testcontainers.PostgreSql
- [ ] `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs` — REG-06 end-to-end

Framework setup needed:
- [ ] Wire `IKeycloakUserService` mock in `WebApplicationFactory<Program>` ConfigureTestServices
- [ ] Wire `IClientRepository` mock OR use Testcontainers.PostgreSql for WebApplicationFactory

*(Existing xUnit/Shouldly/NSubstitute/Mvc.Testing infrastructure covers unit and API test layers)*

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| FluentValidation.AspNetCore auto-pipeline | Manual `IValidator<T>` injection | FV 11→12 (2024) | Controllers must inject validator explicitly |
| `KEYCLOAK_ADMIN` env var | `KC_BOOTSTRAP_ADMIN_USERNAME` | Keycloak 26.x | Already used in Phase 1 (noted in STATE.md) |
| Duende.AccessTokenManagement 2.x API | 4.x API (different method names) | 2024 | `AddClientCredentialsTokenManagement()` is current |

**Deprecated/outdated:**
- `FluentValidation.AspNetCore`: no longer maintained; FV 12 docs say "don't use"
- `OwnsOne` for simple value objects: works but creates complex shadow FK joins; `HasConversion` is cleaner for single-column value types

---

## Open Questions

1. **Keycloak username vs email conflict**
   - What we know: Realm config uses email as username; Keycloak enforces uniqueness on username globally
   - What's unclear: If we use email as username, does Keycloak also deduplicate on email independently?
   - Recommendation: Treat Keycloak 409 from CreateUser as a duplicate signal regardless; compensation path handles it

2. **IClientRepository.DeleteAsync addition**
   - What we know: The interface currently has no delete method
   - What's unclear: Will adding `DeleteAsync` break existing mocked tests in Phase 3?
   - Recommendation: Add `DeleteAsync` to `IClientRepository` interface, update NSubstitute mock setup in existing tests (Phase 3 handler tests don't use delete, so only the interface needs updating)

3. **EF Core migration strategy for value object columns**
   - What we know: `HasConversion` with sealed records works but is not tested yet in this codebase
   - What's unclear: Whether EF Core 10 handles `Cpf?` (nullable value object) without special treatment
   - Recommendation: Write the migration, run it against local Postgres first, and verify column nullability before committing

4. **Idempotency key: optional vs required**
   - What we know: REG-08 says "para evitar double-submit" — implies client-driven key
   - What's unclear: Should missing Idempotency-Key be rejected (400) or allowed through?
   - Recommendation: Allow through without caching (the filter already handles this). Document in API contract that the key is optional; without it, double-submit protection is not guaranteed.

---

## Sources

### Primary (HIGH confidence)
- [Keycloak.AuthServices.Sdk NuGet](https://www.nuget.org/packages/Keycloak.AuthServices.Sdk) — confirmed version 2.9.0 (latest as of 2026-04-04)
- [Duende.AccessTokenManagement NuGet](https://www.nuget.org/packages/Duende.AccessTokenManagement) — confirmed version 4.2.0 (Apache 2.0)
- [FluentValidation NuGet](https://www.nuget.org/packages/FluentValidation) — confirmed version 12.1.1 (latest)
- [Testcontainers.Keycloak NuGet](https://www.nuget.org/packages/Testcontainers.Keycloak) — confirmed 4.11.0
- [Testcontainers.PostgreSql NuGet](https://www.nuget.org/packages/Testcontainers.PostgreSql) — confirmed 4.11.0
- [Microsoft.EntityFrameworkCore NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore) — confirmed 10.0.5
- [Npgsql.EntityFrameworkCore.PostgreSQL NuGet](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL) — confirmed 10.0.1
- [EF Core Indexes — Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/modeling/indexes) — HasIndex, IsUnique, HasFilter patterns
- [Milan Jovanovic — Idempotent REST APIs](https://www.milanjovanovic.tech/blog/implementing-idempotent-rest-apis-in-aspnetcore) — IdempotentAttribute filter code

### Secondary (MEDIUM confidence)
- [Keycloak.AuthServices Admin REST API docs](https://nikiforovall.blog/keycloak-authorization-services-dotnet/admin-rest-api/admin-rest-api.html) — AddKeycloakAdminHttpClient setup
- [Keycloak.AuthServices Access Token docs](https://nikiforovall.blog/keycloak-authorization-services-dotnet/admin-rest-api/access-token.html) — AddClientCredentialsTokenManagement pattern
- [GitHub NikiforovAll/keycloak-authorization-services-dotnet](https://github.com/NikiforovAll/keycloak-authorization-services-dotnet) — IKeycloakUserClient interface (source inspection via WebFetch)
- [FluentValidation ASP.NET Core docs](https://docs.fluentvalidation.net/en/latest/aspnet.html) — manual validation approach confirmed

### Tertiary (LOW confidence — needs validation in implementation)
- IKeycloakUserClient exact method signature — verified from GitHub source inspection but not from pinned version tag; verify against installed package
- `GetUsersAsync` by email to retrieve created user's ID — standard pattern but not confirmed in official docs; test this during implementation

---

## Metadata

**Confidence breakdown:**
- Standard stack (packages + versions): HIGH — verified via `dotnet package search` against NuGet live registry
- Architecture (EF Core patterns, controller structure): HIGH — standard ASP.NET Core + EF Core patterns
- Keycloak SDK integration specifics: MEDIUM — GitHub source inspection; not from pinned docs
- Pitfalls: HIGH — based on direct code analysis of this project's existing domain model

**Research date:** 2026-04-04
**Valid until:** 2026-05-04 (Keycloak.AuthServices.Sdk updates frequently — re-check version)
