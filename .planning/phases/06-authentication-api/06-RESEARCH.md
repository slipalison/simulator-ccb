# Phase 6: Authentication API - Research

**Researched:** 2026-04-06
**Domain:** ASP.NET Core JWT Bearer Authentication + Keycloak ROPC Token Exchange
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** `POST /api/auth/login` — recebe `{email, password}`, faz ROPC call ao Keycloak, retorna `{access_token, refresh_token, expires_in, token_type}`. Endpoint público (sem [Authorize]).
- **D-02:** `POST /api/auth/refresh` — recebe `{refresh_token}`, faz refresh call ao Keycloak, retorna novos tokens. Endpoint público (sem [Authorize]).
- **D-03:** `GET /api/clients/me` — rota protegida com `[Authorize]`. Retorna perfil do cliente autenticado. Retorna 401 se token ausente/inválido/expirado.
- **D-04:** `AddAuthentication().AddJwtBearer()` configurado com `Authority = Keycloak:RealmUrl`. Auto-discovery via OIDC metadata endpoint.
- **D-05:** `ValidateAudience = false` — tokens ROPC do Keycloak têm `aud: ["account"]` ou `aud: ["onboarding-app", "account"]`, não o nosso API.
- **D-06:** `[Authorize]` sem policy explícita — apenas validação de token JWT assinado e não expirado.
- **D-07:** Lookup por `email` claim do JWT em `GET /api/clients/me`.
- **D-08:** Adicionar `GetByEmailAsync` ao `IClientRepository` + implementação em `ClientRepository`. Sem migration.
- **D-09:** Retorna 404 com ProblemDetails genérico se nenhum cliente encontrado.
- **D-10:** Nova interface `IKeycloakTokenService` na camada Application. Métodos: `ExchangePasswordAsync(email, password, ct)` e `RefreshTokenAsync(refreshToken, ct)`.
- **D-11:** Implementação `KeycloakTokenService` na Infrastructure via `IHttpClientFactory` (named client `"keycloak-token"`). POST para `{RealmUrl}/protocol/openid-connect/token`.
- **D-12:** `client_id = onboarding-app` (public client, sem secret). Lido de `Keycloak:PublicClientId`.
- **D-13:** Erros Keycloak (401 invalid_grant) → retornar 401 com ProblemDetails genérico (`"Invalid credentials"`).
- **D-14:** Backend só expõe `POST /api/auth/refresh`. A detecção de "token quase expirado" é responsabilidade do frontend (Phase 9).

### Claude's Discretion

- Estrutura exata do `TokenResponse` DTO (incluir `expires_in`, `token_type`, etc.)
- Configuração do timeout do `HttpClient` para o Keycloak token endpoint
- Estrutura de pastas para `AuthController` (junto com `RegistrationController` em `Controllers/`)

### Deferred Ideas (OUT OF SCOPE)

- Detecção de "token quase expirado" e refresh automático — Phase 9 (frontend)
- Logout endpoint (revogar refresh_token no Keycloak) — não está em AUTH-02/03/04
- Rate limiting no endpoint de login — BACK-07 ou fase de hardening
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| AUTH-02 | Token JWT (access + refresh) retornado após login bem-sucedido | `POST /api/auth/login` faz ROPC call ao Keycloak via `IKeycloakTokenService.ExchangePasswordAsync`; retorna `TokenResponse` com `access_token`, `refresh_token`, `expires_in`, `token_type` |
| AUTH-03 | Rota /profile protegida — redireciona para login se não autenticado | `GET /api/clients/me` com `[Authorize]` retorna 401 automaticamente se Bearer token ausente/inválido. Em API pura, não há redirect — 401 é o comportamento correto para SPA. |
| AUTH-04 | Token refresh automático quando access_token próximo da expiração | `POST /api/auth/refresh` expõe o refresh endpoint no backend. A lógica de detecção e trigger fica no frontend (Phase 9) — conforme D-14. |
</phase_requirements>

---

## Summary

A Phase 6 implementa a camada de autenticação no backend ASP.NET Core. O trabalho se divide em três partes ortogonais: (1) configurar `AddJwtBearer` no `Program.cs` para validar tokens emitidos pelo Keycloak via OIDC auto-discovery; (2) criar `AuthController` com endpoints públicos de login e refresh que fazem proxy das chamadas ROPC ao Keycloak via `IKeycloakTokenService`; (3) adicionar `GetByEmailAsync` ao `IClientRepository` para que `GET /api/clients/me` consiga recuperar o perfil do cliente autenticado.

O padrão `AddJwtBearer` com `Authority` lendo o OIDC discovery document (`/.well-known/openid-configuration`) é o caminho idiomático — sem necessidade de configurar chaves de assinatura manualmente. O middleware valida automaticamente assinatura, expiração e issuer. A configuração `ValidateAudience = false` é necessária porque tokens ROPC do Keycloak não contêm o audience da nossa API.

Para testes, o padrão estabelecido em fases anteriores (`RegistrationTestApiFactory` com NSubstitute) se aplica diretamente. A proteção de rotas em testes usa `PostConfigure<JwtBearerOptions>` para desabilitar validação de assinatura e emitir tokens falsos via `JwtSecurityTokenHandler`. Esse padrão é bem documentado e funciona sem dependência de containers.

**Primary recommendation:** Implementar `AddJwtBearer` com Authority no `Program.cs`, criar `IKeycloakTokenService` seguindo o padrão `IKeycloakUserService` existente, e expandir `IClientRepository` com `GetByEmailAsync` — tudo sem nova migration nem modificação no aggregate `Client`.

---

## Project Constraints (from CLAUDE.md)

Diretivas do `CLAUDE.md` que o planejador DEVE verificar antes de aprovar qualquer task:

| Diretiva | Aplicação nesta fase |
|----------|----------------------|
| **Controllers ASP.NET (sem Minimal API)** | `AuthController` DEVE usar `[ApiController]` + `[Route("api/[controller]")]` |
| **Sem MediatR** | Commands/queries via `ICommandHandler` / `IQueryHandler` injetados diretamente |
| **Sem FluentAssertions** — usar Shouldly | Todos os asserts nos novos testes usam Shouldly |
| **Sem Moq** — usar NSubstitute | Mocks de `IKeycloakTokenService`, `IClientRepository` com NSubstitute |
| **Sem keycloak-js** | Não se aplica (backend apenas nesta fase) |
| **Sem localStorage para tokens** | Não se aplica (backend apenas nesta fase) |
| **Sem ASP.NET Core Identity** | Keycloak é o único identity provider — não adicionar Identity |
| **Sem Duende.AccessTokenManagement para user tokens** | Explicitamente proibido pelo CONTEXT.md D-11/D-12: usar `IHttpClientFactory` direto para ROPC |
| **Serilog + OpenTelemetry obrigatórios desde o início** | Já configurados — requests de login serão logados via `UseSerilogRequestLogging()` automaticamente |
| **TDD** | Wave 0 com stubs RED antes de implementação GREEN |
| **OSS-only (MIT/Apache 2.0)** | `Microsoft.AspNetCore.Authentication.JwtBearer` — incluído no .NET SDK, licença MIT |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | **10.0.5** | Valida Bearer tokens JWT nas requisições | Built-in no SDK .NET 10; padrão de mercado para APIs ASP.NET Core |
| `System.IdentityModel.Tokens.Jwt` | transitivo via JwtBearer | Parsing/geração de JWT em testes | Transitivo — não adicionar explicitamente |
| `IHttpClientFactory` (built-in) | built-in .NET 10 | Named client `"keycloak-token"` para ROPC calls | Já em uso no projeto para Admin API |

**Versão verificada:** `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.5, lançado em 12/03/2026. [VERIFIED: nuget.org]

### Pacotes já presentes no projeto (sem nova instalação)

| Library | Versão no projeto | Uso na Phase 6 |
|---------|-------------------|----------------|
| `Serilog.AspNetCore` | 10.0.0 | `UseSerilogRequestLogging()` — já ativo, cobre login automaticamente |
| `NSubstitute` | 5.3.0 | Mocks de `IKeycloakTokenService` em testes |
| `Shouldly` | 4.3.0 | Asserts nos novos testes |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.0 | `WebApplicationFactory` para testes de integração |
| `xUnit` | 2.9.3 | Framework de testes |

### Instalação necessária

Adicionar ao `Onboarding.API.csproj`:

```bash
dotnet add src/Onboarding.API/Onboarding.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.5
```

> Nenhum outro pacote novo é necessário para esta fase.

---

## Architecture Patterns

### Estrutura de pastas recomendada

```
src/Onboarding.Application/
├── Auth/
│   ├── Commands/
│   │   ├── LoginCommand.cs
│   │   ├── LoginCommandHandler.cs
│   │   ├── RefreshTokenCommand.cs
│   │   └── RefreshTokenCommandHandler.cs
│   ├── DTOs/
│   │   └── TokenResponse.cs
│   └── Validators/
│       ├── LoginCommandValidator.cs
│       └── RefreshTokenCommandValidator.cs
├── Common/
│   └── IKeycloakTokenService.cs   ← nova interface

src/Onboarding.Infrastructure/
└── Keycloak/
    └── KeycloakTokenService.cs    ← nova implementação

src/Onboarding.Domain/
└── Repositories/
    └── IClientRepository.cs       ← + GetByEmailAsync

src/Onboarding.Infrastructure/
└── Repositories/
    └── ClientRepository.cs        ← + GetByEmailAsync impl

src/Onboarding.API/
└── Controllers/
    └── AuthController.cs          ← POST /api/auth/login, POST /api/auth/refresh

tests/Onboarding.API.Tests/
└── Auth/
    ├── AuthControllerTests.cs
    └── AuthTestApiFactory.cs

tests/Onboarding.Domain.Tests/
└── Application/
    └── Commands/
        ├── LoginCommandHandlerTests.cs
        └── RefreshTokenCommandHandlerTests.cs
```

### Padrão 1: Configuração JWT Bearer com OIDC Auto-Discovery

**O que é:** `AddJwtBearer` lê o OIDC discovery document do Keycloak automaticamente para obter a chave pública de assinatura, issuer, e endpoints. Sem configuração manual de chaves.

**Quando usar:** Sempre que o Identity Provider suporta OIDC (Keycloak suporta — endpoint: `{RealmUrl}/.well-known/openid-configuration`).

```csharp
// Fonte: [CITED: learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication]
// Em Program.cs, ANTES de builder.Services.AddControllers()
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Auto-discovery: lê {RealmUrl}/.well-known/openid-configuration
        options.Authority = builder.Configuration["Keycloak:RealmUrl"]
            ?? throw new InvalidOperationException("Keycloak:RealmUrl not configured.");

        // D-05: tokens ROPC têm aud: ["account"] ou ["onboarding-app", "account"]
        // Não têm o audience da nossa API — desabilitar para evitar 401 falso positivo
        options.TokenValidationParameters.ValidateAudience = false;

        // D-04: MapInboundClaims = false preserva os claim names do Keycloak
        // sem remapeamento automático (ex: "email" permanece "email", não "emailaddress")
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();
```

```csharp
// Em app.Use*() pipeline — OBRIGATÓRIO antes de MapControllers()
app.UseAuthentication();   // ← novo em Phase 6
app.UseAuthorization();    // ← novo em Phase 6
app.MapControllers();
```

**Pitfall crítico de ordem:** `UseAuthentication()` DEVE vir antes de `UseAuthorization()` que DEVE vir antes de `MapControllers()`. Ordem errada resulta em 401 em todas as rotas ou autorização ignorada silenciosamente. [CITED: docs MS]

### Padrão 2: IKeycloakTokenService — Interface na Application Layer

**O que é:** Abstração que desacopla a lógica HTTP de chamada ao Keycloak do handler. Segue o mesmo padrão de `IKeycloakUserService`.

```csharp
// src/Onboarding.Application/Common/IKeycloakTokenService.cs
namespace Onboarding.Application.Common;

public interface IKeycloakTokenService
{
    Task<TokenResponse> ExchangePasswordAsync(
        string email, string password, CancellationToken ct = default);

    Task<TokenResponse> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);
}
```

```csharp
// src/Onboarding.Application/Auth/DTOs/TokenResponse.cs
// [Claude's Discretion] — incluir todos os campos da resposta Keycloak para compatibilidade
namespace Onboarding.Application.Auth.DTOs;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    int RefreshExpiresIn,
    string Scope);
```

### Padrão 3: KeycloakTokenService — ROPC via IHttpClientFactory

**O que é:** Implementação Infrastructure que faz POST form-encoded ao endpoint de token do Keycloak.

```csharp
// src/Onboarding.Infrastructure/Keycloak/KeycloakTokenService.cs
// Source: Keycloak OIDC token endpoint spec + padrão IHttpClientFactory já em uso no projeto
public sealed class KeycloakTokenService : IKeycloakTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _realmUrl;
    private readonly string _clientId;

    public KeycloakTokenService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _realmUrl = configuration["Keycloak:RealmUrl"]
            ?? throw new InvalidOperationException("Keycloak:RealmUrl not configured.");
        _clientId = configuration["Keycloak:PublicClientId"]
            ?? throw new InvalidOperationException("Keycloak:PublicClientId not configured.");
    }

    public async Task<TokenResponse> ExchangePasswordAsync(
        string email, string password, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("keycloak-token");
        var response = await client.PostAsync(
            $"{_realmUrl}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _clientId,
                ["username"] = email,
                ["password"] = password,
                ["scope"] = "openid email profile"
            }),
            ct);

        if (!response.IsSuccessStatusCode)
            throw new KeycloakAuthException("Invalid credentials.");

        return await DeserializeTokenResponse(response, ct);
    }

    public async Task<TokenResponse> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("keycloak-token");
        var response = await client.PostAsync(
            $"{_realmUrl}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _clientId,
                ["refresh_token"] = refreshToken
            }),
            ct);

        if (!response.IsSuccessStatusCode)
            throw new KeycloakAuthException("Invalid or expired refresh token.");

        return await DeserializeTokenResponse(response, ct);
    }

    private static async Task<TokenResponse> DeserializeTokenResponse(
        HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadFromJsonAsync<KeycloakTokenJson>(ct)
            ?? throw new InvalidOperationException("Empty token response from Keycloak.");
        return new TokenResponse(
            json.AccessToken,
            json.RefreshToken,
            json.ExpiresIn,
            json.TokenType,
            json.RefreshExpiresIn,
            json.Scope ?? string.Empty);
    }
}

// DTO de desserialização interna — mapeado da resposta JSON do Keycloak
internal sealed record KeycloakTokenJson(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope);
```

**Registro no DependencyInjection.cs:**
```csharp
// Adicionar em AddInfrastructure()
// Named client sem auth handler — chamadas ROPC não carregam Bearer token de saída
services.AddHttpClient("keycloak-token", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);  // [Claude's Discretion] timeout razoável
});
services.AddScoped<IKeycloakTokenService, KeycloakTokenService>();
```

**Adicionar ao appsettings.json:**
```json
"Keycloak": {
  "PublicClientId": "onboarding-app"
}
```

### Padrão 4: AuthController — Endpoints públicos de auth

**O que é:** Controller seguindo exatamente o padrão de `RegistrationController`. Endpoints públicos (sem `[Authorize]` na classe).

```csharp
// Source: padrão RegistrationController existente no projeto
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    { ... }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    { ... }
}
```

### Padrão 5: ClientsController com GET /api/clients/me

**O que é:** Controller novo (ou pode ser `ClientsController`) com rota protegida.

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController : ControllerBase
{
    // GET /api/clients/me — AUTH-03: protegido, retorna 401 se sem Bearer
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        // D-07: lookup por email claim do JWT
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized();

        var query = new GetClientByEmailQuery(email);
        var result = await _queryHandler.HandleAsync(query, ct);
        if (result is null)
            return NotFound(new ProblemDetails
            {
                Title = "Client not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "No client profile found for this account."  // D-09: genérico
            });

        return Ok(result);
    }
}
```

### Padrão 6: GetByEmailAsync no Repository

```csharp
// IClientRepository — adicionar:
Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default);

// ClientRepository — implementação (normalização já existe no ExistsByEmailAsync):
public async Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default)
{
    var normalized = email.ToLowerInvariant();
    return await _db.Clients
        .FirstOrDefaultAsync(c => c.Email.Value == normalized, ct);
}
```

### Padrão 7: Teste de endpoint protegido com JWT falso

**O que é:** Padrão para testar `GET /api/clients/me` sem Keycloak real. Usa `PostConfigure<JwtBearerOptions>` para desabilitar validação de assinatura e emite JWT unsigned com `JwtSecurityTokenHandler`.

```csharp
// [CITED: renatogolia.com/2025/08/01/testing-aspnet-core-endpoints-with-fake-jwt-tokens-and-webapplicationfactory/]
// Em AuthTestApiFactory.ConfigureTestServices:
services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidateIssuerSigningKey = false;
    options.TokenValidationParameters.ValidateIssuer = false;
    options.TokenValidationParameters.ValidateAudience = false;
    options.TokenValidationParameters.ValidateLifetime = false;
    options.TokenValidationParameters.RequireSignedTokens = false;
});

// Helper para gerar token de teste:
private static string GenerateFakeJwt(string email)
{
    var token = new JwtSecurityToken(
        issuer: "http://localhost",
        audience: "http://localhost",
        expires: DateTime.UtcNow.AddHours(1),
        claims: [new Claim("email", email)]);
    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Uso nos testes:
_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", GenerateFakeJwt("joao@example.com"));
```

**Por que `PostConfigure` e não `Configure`:** `PostConfigure` é aplicado DEPOIS das configurações do app, garantindo que override funciona. `Configure` pode ser sobrescrito silenciosamente. [CITED: renatogolia.com]

### Anti-Patterns a Evitar

- **[Usar `Duende.AccessTokenManagement` para tokens do usuário]:** O projeto já tem `Duende.AccessTokenManagement` mas exclusivamente para o service account (CC grant). NÃO usar para tokens ROPC do usuário — decisão explícita no CONTEXT.md D-11.
- **[Adicionar `[Authorize]` na classe `AuthController`]:** Login e refresh são endpoints públicos. O atributo fica na classe `ClientsController` ou apenas no método `GetMe`.
- **[Usar `User.Identity.Name` para obter email]:** Em tokens Keycloak com `MapInboundClaims = false`, `User.Identity.Name` pode ser `null` ou `sub`. Usar `User.FindFirst("email")?.Value` conforme D-07.
- **[`UseAuthorization()` antes de `UseAuthentication()`]:** Resulta em comportamento indefinido. Ordem correta: `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`.
- **[`MapInboundClaims = true` (default)]:** Com o default `true`, o middleware remapeia `email` para `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`. Com `MapInboundClaims = false`, o claim permanece como `"email"` — consistente com o que o Keycloak envia.

---

## Don't Hand-Roll

| Problema | Não Construir | Usar Em Vez Disso | Por Quê |
|----------|---------------|-------------------|---------|
| Validação de assinatura JWT | Verificação manual de HMAC/RSA | `AddJwtBearer` com `Authority` | Auto-discovery de chaves públicas, rotação automática, validação completa de claims |
| Parsing de JWT claims | `base64decode(token.split('.')[1])` | `User.FindFirst("email")` | Thread-safe, respeita MapInboundClaims, type-safe |
| Cache de chaves públicas | Armazenar localmente a chave pública | `AddJwtBearer` | O middleware gerencia cache e rotação automática |
| Verificação de expiração de token | Comparar `exp` manualmente | `ValidateLifetime = true` (default) | O middleware já faz isso — e considera clock skew |

**Key insight:** O `JwtBearerHandler` do ASP.NET Core faz auto-discovery, caching de chave pública, validação de assinatura, expiração e issuer em uma única chamada de configuração. Qualquer reimplementação manual dessas responsabilidades introduz riscos de segurança.

---

## Common Pitfalls

### Pitfall 1: Ordem do middleware de autenticação/autorização

**O que dá errado:** `UseAuthorization()` antes de `UseAuthentication()` — todas as rotas com `[Authorize]` retornam 401 mesmo com token válido, ou autorização é ignorada silenciosamente.

**Por que acontece:** `UseAuthentication()` popula `HttpContext.User`. Se `UseAuthorization()` rodar antes, o principal ainda é anônimo.

**Como evitar:** Ordem obrigatória no `Program.cs`:
```csharp
app.UseSerilogRequestLogging();
app.UseAuthentication();   // ← deve vir antes
app.UseAuthorization();    // ← deve vir depois
app.MapControllers();
```

**Warning signs:** Rota com `[Authorize]` retornando 401 mesmo enviando Bearer token válido.

### Pitfall 2: MapInboundClaims = true (default) quebra lookup de email

**O que dá errado:** `User.FindFirst("email")` retorna `null` porque o middleware remapeou `"email"` para o namespace longo XML (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`).

**Por que acontece:** `JwtBearerOptions.MapInboundClaims` é `true` por default em versões antigas. No .NET 10 o default pode variar por versão — é mais seguro setar explicitamente.

**Como evitar:** Setar `options.MapInboundClaims = false` na configuração do `AddJwtBearer`. Verificado: o CONTEXT.md já registra essa necessidade (D-07 usa `User.FindFirst("email")`).

**Warning signs:** `User.FindFirst("email")` retorna null em produção mas funciona em testes com token falso onde o claim foi adicionado manualmente com o nome curto.

### Pitfall 3: ValidateAudience = true com tokens ROPC do Keycloak

**O que dá errado:** Todos os requests retornam 401 com erro `"The audience 'account' is invalid"` — o token válido é rejeitado.

**Por que acontece:** Tokens ROPC do Keycloak têm `aud: ["account"]` ou `aud: ["onboarding-app", "account"]` por padrão — não incluem o audience da API (que o Keycloak só adicionaria com Resource Indicators ou audience mapper explícito).

**Como evitar:** `options.TokenValidationParameters.ValidateAudience = false` conforme D-05. Confirmado: o `onboarding-realm.json` mostra `defaultClientScopes: ["openid", "profile", "email"]` sem audience mapper para a API. [VERIFIED: keycloak/onboarding-realm.json]

**Warning signs:** 401 com mensagem sobre audience inválido nos logs do `Microsoft.AspNetCore.Authentication.JwtBearer`.

### Pitfall 4: OIDC Discovery document indisponível na inicialização

**O que dá errado:** A aplicação falha ao iniciar (ou nos primeiros requests) porque o Keycloak ainda não está pronto — o `AddJwtBearer` com `Authority` tenta fazer fetch do discovery document no primeiro request, não na inicialização.

**Por que acontece:** O `JwtBearerHandler` faz lazy-loading do discovery document. Em containers, o Keycloak pode demorar para estar pronto.

**Como evitar:** O `docker-compose.yaml` já tem `depends_on: keycloak: condition: service_healthy`. Para testes, o `PostConfigure` desabilita a validação — sem chamada ao Keycloak. [VERIFIED: padrão existente no projeto]

**Warning signs:** Logs com `IDX20803: Unable to obtain configuration from 'http://keycloak:8080/realms/onboarding/.well-known/openid-configuration'` nos primeiros requests após deploy.

### Pitfall 5: SensitiveDataDestructuringPolicy e o campo `password` em login

**O que dá errado:** A senha do usuário aparece nos logs de request estruturado.

**Por que não ocorre aqui:** A `SensitiveDataDestructuringPolicy` já mascara `password` e `token` globalmente (Phase 4). O `UseSerilogRequestLogging()` não loga o body — apenas method, path, status e duration. Mas se alguém adicionar log manual do request body, a policy cobre.

**Como verificar:** O test `SensitiveDataDestructuringPolicyTests.cs` existente cobre esse comportamento.

### Pitfall 6: IdempotencyFilter em endpoints de auth

**O que dá errado:** `[Idempotent]` aplicado em `POST /api/auth/login` ou `POST /api/auth/refresh` — segundo login com a mesma idempotency key retorna o access token do primeiro request (já expirado).

**Como evitar:** NUNCA aplicar `[Idempotent]` em endpoints de auth. Idempotência para autenticação não faz sentido semântico. O CONTEXT.md já registra esse ponto explicitamente.

---

## Keycloak Token Response Format

Formato verificado da resposta do endpoint `POST {realm}/protocol/openid-connect/token` com `grant_type=password`:

```json
{
  "access_token": "<JWT>",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "refresh_token": "<JWT>",
  "token_type": "Bearer",
  "not-before-policy": 0,
  "session_state": "<UUID>",
  "scope": "openid email profile"
}
```

[CITED: appsdeveloperblog.com/keycloak-requesting-token-with-password-grant/ + VERIFIED: onboarding-realm.json `accessTokenLifespan: 300`]

**Claims no access token JWT do Keycloak (com `email` scope ativo):**
- `sub` — Keycloak user UUID
- `iss` — `http://keycloak:8080/realms/onboarding`
- `aud` — `["account"]` (ou `["onboarding-app", "account"]`)
- `email` — email do usuário (disponível porque `email` está em `defaultClientScopes` do `onboarding-app`) [VERIFIED: keycloak/onboarding-realm.json]
- `preferred_username` — username (= email no nosso realm)
- `exp`, `iat`, `jti` — campos padrão OAuth 2.0

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Configurar chave de assinatura manualmente em `JwtBearerOptions` | `Authority` com auto-discovery via OIDC | ASP.NET Core 2.0+ | Sem necessidade de gerenciar chaves manualmente |
| `MapInboundClaims = true` (default histórico) | `MapInboundClaims = false` | .NET 6+ recomendado | Preserva claim names originais do IdP |
| `UseJwtBearerAuthentication()` (obsoleto) | `AddAuthentication().AddJwtBearer()` | ASP.NET Core 2.0 | API atual |

**Deprecated/outdated:**
- `WebMotions.Fake.Authentication.JwtBearer` NuGet: alternativa válida mas `PostConfigure<JwtBearerOptions>` é mais simples e sem dependência extra para este projeto.

---

## Assumptions Log

| # | Claim | Section | Risk se Errado |
|---|-------|---------|----------------|
| A1 | `email` claim estará presente no access token JWT com o scope `email` ativo no `onboarding-app` | Keycloak Token Response Format | Se ausente, `GET /api/clients/me` retorna 401/404 para todos os usuários. Mitigação: verificar via curl após deploy. |
| A2 | `MapInboundClaims = false` é necessário para acessar claim `"email"` diretamente | Padrão 1 + Pitfall 2 | Se o default do .NET 10 já for `false`, não é problema. Se `true`, o lookup por `email` falha. Mitigação: setar explicitamente. |

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Compilação | ✓ | 10.0.201 | — |
| Docker | Integration tests com Testcontainers | ✓ | verificado em fases anteriores | — |
| Keycloak container (`quay.io/keycloak/keycloak:26.1`) | Integration tests | ✓ | 26.1 (usado em fases anteriores) | — |
| PostgreSQL container (`postgres:16-alpine`) | Integration tests | ✓ | 16-alpine (usado em fases anteriores) | — |

Step 2.6: Nenhuma dependência nova externa identificada. Todas já validadas em fases anteriores.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | `tests/Onboarding.API.Tests/` e `tests/Onboarding.Integration.Tests/` (existentes) |
| Quick run command | `dotnet test tests/Onboarding.API.Tests/ --filter "Category!=Integration" --no-build` |
| Full suite command | `dotnet test tests/ --no-build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Existe? |
|--------|----------|-----------|-------------------|-------------|
| AUTH-02 | POST /api/auth/login com credenciais válidas retorna 200 com access_token e refresh_token | Unit (mock IKeycloakTokenService) | `dotnet test tests/Onboarding.API.Tests/ --filter "FullyQualifiedName~AuthControllerTests"` | ❌ Wave 0 |
| AUTH-02 | POST /api/auth/login com credenciais inválidas retorna 401 com ProblemDetails genérico | Unit (mock lança KeycloakAuthException) | idem | ❌ Wave 0 |
| AUTH-02 | LoginCommand com email/password vazios retorna 422 (FluentValidation) | Unit | idem | ❌ Wave 0 |
| AUTH-03 | GET /api/clients/me sem Bearer token retorna 401 | Unit (WebApplicationFactory, sem token) | `dotnet test tests/Onboarding.API.Tests/ --filter "FullyQualifiedName~ClientsControllerTests"` | ❌ Wave 0 |
| AUTH-03 | GET /api/clients/me com Bearer válido e cliente existente retorna 200 com perfil | Unit (JWT fake + mock IClientRepository) | idem | ❌ Wave 0 |
| AUTH-03 | GET /api/clients/me com Bearer válido mas email não cadastrado retorna 404 | Unit (JWT fake + mock retorna null) | idem | ❌ Wave 0 |
| AUTH-04 | POST /api/auth/refresh com refresh_token válido retorna novos tokens | Unit (mock IKeycloakTokenService) | `dotnet test tests/Onboarding.API.Tests/ --filter "FullyQualifiedName~AuthControllerTests"` | ❌ Wave 0 |
| AUTH-04 | POST /api/auth/refresh com refresh_token inválido retorna 401 | Unit (mock lança exceção) | idem | ❌ Wave 0 |

### Sampling Rate

- **Por task commit:** `dotnet test tests/Onboarding.API.Tests/ --filter "Category!=Integration" --no-build`
- **Por wave merge:** `dotnet test tests/ --filter "Category!=Integration" --no-build`
- **Phase gate:** Suite completa verde (`dotnet test tests/ --no-build`) antes de `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `tests/Onboarding.API.Tests/Auth/AuthControllerTests.cs` — cobre AUTH-02, AUTH-04
- [ ] `tests/Onboarding.API.Tests/Auth/ClientsControllerTests.cs` — cobre AUTH-03
- [ ] `tests/Onboarding.API.Tests/Auth/AuthTestApiFactory.cs` — factory com `PostConfigure<JwtBearerOptions>` e JWT fake helper
- [ ] `tests/Onboarding.Domain.Tests/Application/Commands/LoginCommandHandlerTests.cs` — unit tests do handler
- [ ] `tests/Onboarding.Domain.Tests/Application/Commands/RefreshTokenCommandHandlerTests.cs` — unit tests do handler

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | Keycloak ROPC — brute force já configurado (Phase 2, SEC-01) |
| V3 Session Management | yes | Tokens com `expires_in: 300s` (5 min). Sem session server-side na API |
| V4 Access Control | yes | `[Authorize]` em `GET /api/clients/me`. Sem roles adicionais (D-06) |
| V5 Input Validation | yes | FluentValidation em `LoginCommand` e `RefreshTokenCommand` |
| V6 Cryptography | yes | Assinatura JWT validada pelo Keycloak via OIDC discovery — nunca hand-roll |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Credential stuffing / brute force | Spoofing | SEC-01 já ativo: Keycloak brute force protection (max 5 falhas, 30s escalating) |
| Information leakage em login | Information Disclosure | D-13: erros Keycloak → 401 genérico `"Invalid credentials"`. Nunca revelar se email existe (SEC-08) |
| JWT token forgery | Spoofing/Tampering | `AddJwtBearer` com `Authority` valida assinatura RS256 usando chave pública do Keycloak |
| Replay de access token expirado | Elevation of Privilege | `ValidateLifetime = true` (default) — tokens expirados retornam 401 |
| Logging de passwords/tokens | Information Disclosure | `SensitiveDataDestructuringPolicy` (Phase 4, SEC-09) mascara globalmente |
| Refresh token theft | Spoofing | Tokens em memória (SEC-10, Phase 9 frontend) — API não armazena tokens |
| SSRF via Keycloak token endpoint URL | SSRF | URL do Keycloak fixada em `appsettings.json` via `Keycloak:RealmUrl` — nunca aceita URL de request do cliente |

---

## Open Questions

1. **`KeycloakAuthException` como exception customizada ou usar `HttpRequestException`?**
   - O que sabemos: `IsTransientKeycloakError` em `RegisterClientCommandHandler` captura `HttpRequestException` para compensação.
   - O que está incerto: Se devemos criar `KeycloakAuthException` nova ou reusar o padrão existente.
   - Recomendação: Criar `KeycloakAuthException : Exception` específica para auth failures — distingue "credenciais inválidas" (401 do Keycloak) de "Keycloak indisponível" (5xx ou timeout). O controller mapeia `KeycloakAuthException` → 401, `HttpRequestException` → 503.

2. **`GetClientByEmailQuery` ou chamada direta ao repository no controller?**
   - O que sabemos: O padrão CQRS existente usa `IQueryHandler<TQuery, TResult>`.
   - O que está incerto: Se o overhead de criar um `GetClientByEmailQuery` + handler justifica a consistência de padrão para uma query simples.
   - Recomendação: Criar `GetClientByEmailQuery` + `GetClientByEmailQueryHandler` para consistência com o padrão CQRS do projeto — facilita testes unitários do handler isoladamente.

---

## Sources

### Primary (HIGH confidence)
- [CITED: learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0] — padrão `AddJwtBearer` com Authority, ValidateAudience, MapInboundClaims, ordem de middleware
- [VERIFIED: nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer] — versão 10.0.5, lançado 12/03/2026
- [VERIFIED: D:/REPO/keycloak-tests/keycloak/onboarding-realm.json] — `directAccessGrantsEnabled: true` em `onboarding-app`, `defaultClientScopes: ["openid", "profile", "email"]`, `accessTokenLifespan: 300`
- [VERIFIED: codebase] — padrões existentes: `IKeycloakUserService`, `RegistrationController`, `ICommandHandler`, `IQueryHandler`, `WebAppFactoryCollection`, `SensitiveDataDestructuringPolicy`

### Secondary (MEDIUM confidence)
- [CITED: renatogolia.com/2025/08/01/testing-aspnet-core-endpoints-with-fake-jwt-tokens-and-webapplicationfactory/] — padrão `PostConfigure<JwtBearerOptions>` para testes com JWT falso (2025, verificado recente)
- [CITED: appsdeveloperblog.com/keycloak-requesting-token-with-password-grant/] — formato de resposta do token endpoint Keycloak (campos `access_token`, `refresh_token`, `expires_in`, `token_type`, `refresh_expires_in`)

### Tertiary (LOW confidence)
- Nenhuma — todas as claims críticas foram verificadas com fontes primárias ou secundárias.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — pacotes verificados no registry, versões confirmadas
- Architecture: HIGH — padrões extraídos diretamente do código existente no projeto
- Pitfalls: HIGH — documentados em fontes oficiais MS + padrões verificados no codebase
- Token format: MEDIUM — verificado via docs externos + realm JSON, mas não via curl ao Keycloak real

**Research date:** 2026-04-06
**Valid until:** 2026-05-06 (stack estável; Keycloak 26.x e .NET 10 com suporte ativo)
