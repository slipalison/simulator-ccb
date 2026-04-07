# Research: Phase 09 — Login UI

**Researched:** 2026-04-07
**Domain:** React 19 Auth State + ROPC Token Exchange + In-Memory JWT Storage + Login Form UX
**Confidence:** HIGH

---

## Standard Stack

| Library | Version | Purpose | Why Standard | Confidence |
|---------|---------|---------|--------------|------------|
| React Context (built-in) | 19.x | Auth state management | STACK.md dictates Context is sufficient. No Redux/Zustand needed for auth + profile view. | HIGH |
| React Hook Form | 7.72.x | Form state + submission | Already installed. Industry standard for performant forms with minimal re-renders. | HIGH |
| @hookform/resolvers/zod | 5.2.x | Zod → RHF adapter | Already installed. Sync validation with Zod schemas shared with API contracts. | HIGH |
| Zod | 4.3.x | Schema validation | Already installed. Type inference (`z.infer`) eliminates DTO duplication. | HIGH |
| Native fetch + interceptors | browser native | HTTP client | ky is NOT installed. Use native `fetch` wrapped in an API client with `hooks.afterResponse` pattern. | MEDIUM |
| LabeledField (existing) | local | Form field component | Already built — molecule with `aria-invalid`, `aria-describedby`, `role="alert"` for errors. | HIGH |
| AppButton (existing) | local | Button component | Already built — atom with loading/disabled states. | HIGH |

### What NOT to Install

| Library | Why Avoid |
|---------|-----------|
| ky | Not in package.json. Adding it for one interceptor pattern is overkill. Native `fetch` + custom wrapper suffices. |
| axios | Heavier than needed. Native `fetch` + `ky.extend()`-style pattern is lighter. |
| keycloak-js | Designed for Authorization Code Flow + PKCE. ROPC grant doesn't use browser redirects. STACK.md explicitly excludes. |
| jwt-decode | Simple `JSON.parse(atob(token.split('.')[1]))` covers exp check. No library needed for one field. |
| React Query / TanStack Query | Over-engineering for this phase. Only 2 mutations (login, refresh) and 1 query (me). Manual `useMutation`-style state in Context is sufficient. |

---

## Architecture Patterns

### Pattern 1: AuthContext + useAuth Hook

**What:** React Context provider wrapping the app, exposing `useAuth()` hook with login, logout, refresh, isAuthenticated, and user state.

**Why:** Single source of truth for auth state. All components access auth via `useAuth()`. Tokens live in module-level variables (not in Context state) to guarantee memory-only storage — Context only exposes derived state (isAuthenticated flag, user info), never raw tokens.

```typescript
// src/frontend/src/auth/AuthContext.tsx
import { createContext, useContext, useState, useCallback, type ReactNode } from "react";

interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;       // timestamp ms
  refreshExpiresAt: number; // timestamp ms
}

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refresh: () => Promise<void>;
  getAccessToken: () => string | null;
}

const AuthContext = createContext<AuthContextType | null>(null);

// Module-level token storage — NOT in React state, NOT in localStorage
let tokens: AuthTokens | null = null;

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const getAccessToken = useCallback(() => tokens?.accessToken ?? null, []);

  const login = useCallback(async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });
      if (!res.ok) {
        const problem = await res.json();
        throw new Error(problem.detail ?? "Login failed");
      }
      const data = await res.json();
      const now = Date.now();
      tokens = {
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        expiresAt: now + data.expiresIn * 1000,
        refreshExpiresAt: now + data.refreshExpiresIn * 1000,
      };
      setIsAuthenticated(true);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const refresh = useCallback(async () => {
    if (!tokens?.refreshToken) throw new Error("No refresh token");
    const res = await fetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: tokens.refreshToken }),
    });
    if (!res.ok) throw new Error("Refresh failed");
    const data = await res.json();
    const now = Date.now();
    tokens = {
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      expiresAt: now + data.expiresIn * 1000,
      refreshExpiresAt: now + data.refreshExpiresIn * 1000,
    };
  }, []);

  const logout = useCallback(() => {
    tokens = null;
    setIsAuthenticated(false);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, refresh, getAccessToken }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
```

**Critical design decision:** Tokens stored in module-level `let tokens` variable, NOT in `useState`. This ensures:
1. Tokens are never serialized to React DevTools
2. Tokens survive re-renders without triggering them
3. `getAccessToken()` is a pure function — no stale closure risk
4. On page refresh, memory is cleared automatically (desired behavior — forces re-auth or silent refresh)

### Pattern 2: Token Refresh with Race Condition Protection

**What:** HTTP interceptor pattern that catches 401 responses, queues concurrent requests, and performs a single token refresh.

**Why:** Without queuing, multiple parallel API calls that receive 401 simultaneously will each trigger their own refresh call, invalidating the refresh token (Keycloak rotates refresh tokens by default).

```typescript
// src/frontend/src/auth/authClient.ts
let isRefreshing = false;
let refreshPromise: Promise<void> | null = null;
let pendingQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: Error) => void;
}> = [];

function drainQueue(error: Error | null, token: string | null): void {
  pendingQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error);
    else resolve(token!);
  });
  pendingQueue = [];
}

async function refreshTokens(refreshToken: string): Promise<void> {
  const res = await fetch("/api/auth/refresh", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
  if (!res.ok) throw new Error("Session expired");
  const data = await res.json();
  const now = Date.now();
  // Update module-level tokens (import from AuthContext or pass via closure)
  // tokens = { ...data, expiresAt: now + data.expiresIn * 1000, ... }
}

export async function authFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = tokens?.accessToken;
  const headers = new Headers(options.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const res = await fetch(url, { ...options, headers });

  if (res.status === 401) {
    if (!tokens?.refreshToken) {
      throw new Error("No refresh token — re-authenticate");
    }

    if (isRefreshing && refreshPromise) {
      // Another request is already refreshing — queue this one
      return new Promise((resolve, reject) => {
        pendingQueue.push({
          resolve: (newToken) => {
            const retryHeaders = new Headers(options.headers);
            retryHeaders.set("Authorization", `Bearer ${newToken}`);
            resolve(fetch(url, { ...options, headers: retryHeaders }));
          },
          reject,
        });
      });
    }

    isRefreshing = true;
    refreshPromise = refreshTokens(tokens.refreshToken)
      .then(() => {
        drainQueue(null, tokens!.accessToken);
      })
      .catch((err) => {
        drainQueue(err, null);
        throw err;
      })
      .finally(() => {
        isRefreshing = false;
        refreshPromise = null;
      });

    await refreshPromise;

    // Retry original request with new token
    const retryHeaders = new Headers(options.headers);
    retryHeaders.set("Authorization", `Bearer ${tokens!.accessToken}`);
    return fetch(url, { ...options, headers: retryHeaders });
  }

  return res;
}
```

### Pattern 3: Proactive Token Refresh (Before Expiry)

**What:** Check token expiration before making requests. If token is within 30 seconds of expiry, refresh proactively rather than waiting for 401.

**Why:** Avoids the round-trip penalty of 401 → refresh → retry. Access token lifespan is 5 minutes — a 30-second buffer is reasonable.

```typescript
function isTokenExpiringSoon(bufferMs = 30_000): boolean {
  if (!tokens) return false;
  return tokens.expiresAt - Date.now() < bufferMs;
}

export async function authFetchWithProactiveRefresh(
  url: string,
  options: RequestInit = {}
): Promise<Response> {
  // Proactive refresh: if token is about to expire, refresh before the request
  if (isTokenExpiringSoon() && tokens?.refreshToken && !isRefreshing) {
    try {
      await refreshTokens(tokens.refreshToken);
    } catch {
      // Token expired — let the 401 handler deal with it
    }
  }

  return authFetch(url, options);
}
```

### Pattern 4: JWT Expiration Check (No Library)

**What:** Decode JWT payload to check `exp` claim without any library.

```typescript
function decodeJwtExp(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.exp; // Unix timestamp seconds
  } catch {
    return null;
  }
}

function isTokenExpired(token: string, bufferMs = 30_000): boolean {
  const exp = decodeJwtExp(token);
  if (!exp) return true;
  return exp * 1000 < Date.now() + bufferMs;
}
```

**Why no library:** The only field we need is `exp`. `jwt-decode` decodes the full header + payload + signature — unnecessary overhead for one field. The `atob` + `JSON.parse` approach is 3 lines, zero dependencies, and works in all browsers.

### Pattern 5: Login Form with Existing Patterns

**What:** Login form reuses `LabeledField` + `React Hook Form` + `Zod` + `AppButton` — exact same stack as `ExampleForm`.

```typescript
// src/frontend/src/components/organisms/LoginForm.tsx
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { LabeledField } from "@/components/molecules/LabeledField";
import { AppButton } from "@/components/atoms/AppButton";
import { useAuth } from "@/auth/AuthContext";

const loginSchema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(1, "Senha é obrigatória"),
});

type LoginData = z.infer<typeof loginSchema>;

export function LoginForm() {
  const { login, isLoading: isAuthLoading } = useAuth();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginData>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = async (data: LoginData) => {
    try {
      await login(data.email, data.password);
      // Navigate to dashboard/profile on success
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Erro ao fazer login";
      // Generic error — do not reveal if email exists (D-13)
      setError("root", { message });
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 w-full max-w-sm" noValidate>
      {errors.root && (
        <p role="alert" className="text-sm text-destructive">
          {errors.root.message}
        </p>
      )}

      <LabeledField
        id="email"
        label="Email"
        error={errors.email?.message}
        inputProps={{
          type: "email",
          placeholder: "seu@email.com",
          autoComplete: "email",
          ...register("email"),
        }}
      />

      <LabeledField
        id="password"
        label="Senha"
        error={errors.password?.message}
        inputProps={{
          type: "password",
          placeholder: "Sua senha",
          autoComplete: "current-password",
          ...register("password"),
        }}
      />

      <AppButton type="submit" disabled={isSubmitting || isAuthLoading} className="w-full">
        {isSubmitting || isAuthLoading ? "Entrando..." : "Entrar"}
      </AppButton>
    </form>
  );
}
```

### Pattern 6: Protected Route Component

**What:** Wraps child components — redirects to login if not authenticated.

```typescript
// src/frontend/src/components/organisms/ProtectedRoute.tsx
import { useAuth } from "@/auth/AuthContext";
import { Navigate } from "@tanstack/react-router";

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Form validation | Custom validation logic | Zod schema + `zodResolver` | Type inference, shared with API contracts, existing pattern |
| Form state management | useState for each field | React Hook Form | Minimizes re-renders, handles submission lifecycle, existing pattern |
| Form field rendering | New input component | `LabeledField` (existing molecule) | Already has `aria-invalid`, `aria-describedby`, `role="alert"` for accessibility |
| Button loading states | New button component | `AppButton` (existing atom) | Already has disabled/loading states |
| JWT decoding | Full JWT parsing library | `JSON.parse(atob(token.split('.')[1]))` | Only need `exp` — 3 lines, zero dependencies |
| Auth provider from scratch | Custom event emitter / pub/sub | React Context + `useContext` | Built-in, type-safe, sufficient for auth + profile scope |
| HTTP client library | Install ky/axios | Native `fetch` + wrapper function | No HTTP client installed yet. One wrapper function covers login, refresh, and API calls. |

---

## Common Pitfalls

### Pitfall 1: Storing tokens in localStorage/sessionStorage

**What goes wrong:** Any XSS vulnerability can read `localStorage` and exfiltrate tokens to attacker-controlled servers. Even "benign" third-party scripts (analytics, chat widgets) can be compromised via supply chain attack.

**Why it happens:** Convenience — localStorage survives page refresh. Memory storage does not.

**How to avoid:** Store tokens ONLY in module-level variables (not React state, not localStorage). On page refresh, the user must re-authenticate OR the app performs a silent refresh using a refresh token (if stored in HttpOnly cookie — not applicable in this ROPC architecture). For this phase: page refresh = logout. This is the correct security tradeoff.

**Warning signs:** Any `localStorage.getItem`, `sessionStorage.setItem`, or persist middleware referencing tokens.

### Pitfall 2: Refresh token race conditions

**What goes wrong:** Multiple concurrent API calls receive 401 simultaneously. Each triggers its own refresh call. Keycloak rotates refresh tokens — the second refresh invalidates the first, causing cascading failures. Eventually all refresh tokens are burned and user is locked out.

**Why it happens:** No synchronization around the refresh operation. Each 401 handler runs independently.

**How to avoid:** Use the `isRefreshing` flag + `pendingQueue` pattern (Pattern 2 above). Only ONE refresh call executes at a time. All other 401s wait for it to complete, then retry with the new token.

**Warning signs:** Multiple `/api/auth/refresh` calls visible in network tab after a single token expiry event.

### Pitfall 3: Not handling brute force protection errors

**What goes wrong:** Keycloak brute force protection locks the account after 5 failed attempts (30s escalating wait). The error returned is `invalid_grant` — same as wrong password. The frontend shows "Invalid credentials" generically, but the user sees nothing indicating they should wait.

**Why it happens:** The backend (D-13) returns generic 401 "Invalid credentials" to avoid email enumeration. The frontend has no way to distinguish between "wrong password" and "account temporarily locked."

**How to avoid (for now):** Show the generic error message. In a future phase, the backend could return a `Retry-After` header or specific error code for locked accounts. For Phase 9, the generic message is sufficient — the user will eventually retry and succeed after the lockout expires.

**Warning signs:** User reports "I keep typing the right password but it won't work" — could be lockout, not wrong credentials.

### Pitfall 4: Stale closures in async login handlers

**What goes wrong:** `login` function captures old `tokens` reference in closure. After successful login, `getAccessToken()` returns `null` because the closure captured the pre-login state.

**Why it happens:** React closures capture values at render time. If `login` is memoized with `useCallback` and captures `tokens` from initial render, it never sees the updated value.

**How to avoid:** Store tokens in module-level `let` variable (Pattern 1), NOT in React state. `getAccessToken()` reads from the mutable module variable, so it always returns the current value regardless of closure timing.

**Warning signs:** `getAccessToken()` returns `null` immediately after successful login, but `isAuthenticated` is `true`.

### Pitfall 5: Not using `noValidate` on forms

**What goes wrong:** Browser's native HTML5 validation fires before Zod validation. User sees browser-native error tooltips that don't match the design system. Zod validation never runs because the form never submits.

**Why it happens:** Missing `noValidate` attribute on `<form>`.

**How to avoid:** Always add `noValidate` to forms using React Hook Form + Zod. Let Zod handle all validation consistently.

**Warning signs:** Browser-native "Please fill out this field" tooltips appearing instead of styled Zod error messages.

### Pitfall 6: Token in React DevTools

**What goes wrong:** Developer opens React DevTools in production, sees the access token in the AuthProvider's state, copies it.

**Why it happens:** Storing tokens in `useState` makes them visible in React DevTools.

**How to avoid:** Pattern 1 stores tokens in module-level `let` variable, not in React state. The Context only exposes `isAuthenticated` (boolean) and `getAccessToken()` (function) — neither reveals the raw token in DevTools.

**Warning signs:** Tokens visible when inspecting AuthProvider in React DevTools Components panel.

---

## Code Examples

### Example 1: Complete Login Flow

```
1. User enters email + password → submits form
2. React Hook Form validates with Zod → if invalid, shows field errors
3. If valid, calls auth.login(email, password)
4. AuthContext.login() → POST /api/auth/login with JSON body
5. Backend validates → calls Keycloak ROPC → returns TokenResponse (camelCase)
6. AuthContext stores tokens in module-level variable
7. AuthContext sets isAuthenticated = true
8. LoginForm navigates to /profile
9. ProtectedRoute renders profile content
10. Profile page calls GET /api/clients/me via authFetch()
11. authFetch() adds Authorization: Bearer {accessToken} header
12. If 401 → authFetch() calls POST /api/auth/refresh → retries with new token
```

### Example 2: Error Handling for Brute Force Lockout

```typescript
// Keycloak returns 401 with "invalid_grant" for both:
// - Wrong password
// - Account temporarily locked (brute force protection)
// Backend (D-13) normalizes both to: { title: "Authentication failed", detail: "Invalid credentials." }

// Frontend shows generic message — no way to distinguish
setError("root", { message: "Credenciais inválidas. Verifique seu email e senha." });

// If user keeps retrying during lockout, they'll keep seeing this message.
// The lockout duration starts at 30s and escalates.
// Future improvement: backend returns Retry-After header for lockout cases.
```

### Example 3: Logout Clears All Auth State

```typescript
const logout = useCallback(() => {
  tokens = null;           // Clear module-level tokens
  setIsAuthenticated(false); // Trigger re-render of protected routes
  // No need to call backend — refresh token is in memory only and will expire naturally
  // No localStorage/sessionStorage to clear
  // Navigate to login
}, []);
```

### Example 4: Page Refresh = Session Lost (By Design)

```
1. User is logged in, has valid tokens in memory
2. User presses F5 / Ctrl+R
3. Browser unloads all JavaScript — module-level `tokens` variable is destroyed
4. App re-renders, AuthProvider initializes with tokens = null, isAuthenticated = false
5. ProtectedRoute redirects to /login
6. User must log in again

This is the CORRECT behavior for memory-only token storage.
Tradeoff: convenience (re-login) vs security (no XSS token theft).
For this phase, security wins.
Future (v2): HttpOnly cookie with refresh token enables silent session recovery.
```

---

## Migration Path

### ROPC → Auth Code + PKCE for OAuth 2.1

**Current state:** ROPC grant is deprecated in OAuth 2.1. The IETF OAuth Working Group has published "OAuth 2.1" as a Standards Track RFC that explicitly removes ROPC from the specification. Keycloak 26.x still supports it (Direct Access Grants), but the feature is disabled by default for new clients and the Keycloak team has discussed feature-flagging it.

**When to migrate:** Not urgent. ROPC works for first-party apps where the backend is the only client communicating with Keycloak. Migrate when:
- Security audit requires OAuth 2.1 compliance
- MFA is required (ROPC doesn't support it)
- Federation with external IdPs (Google, Microsoft) is needed

**What changes:**

| Aspect | Current (ROPC) | Future (Auth Code + PKCE) |
|--------|---------------|---------------------------|
| Flow | Frontend → Backend → Keycloak (token endpoint) | Frontend → Keycloak (authorize endpoint, browser redirect) → Backend |
| Keycloak client config | `onboarding-app` with Direct Access Grants | `onboarding-app` with Standard Flow + PKCE |
| Backend role | Proxies credentials to Keycloak token endpoint | Exchanges authorization code for tokens |
| Frontend role | Collects email + password in custom form | Redirects to Keycloak login page |
| keycloak-js | Not needed | Can be used (designed for this flow) |
| MFA support | No | Yes |
| Federation support | No | Yes |

**Backend migration (AuthController):**

```typescript
// CURRENT (ROPC):
// POST /api/auth/login → IKeycloakTokenService.ExchangePasswordAsync(email, password)
//   → POST {keycloak}/token with grant_type=password

// FUTURE (Auth Code + PKCE):
// GET /api/auth/login → Generate code_verifier + code_challenge
//   → 302 Redirect to {keycloak}/authorize?response_type=code&client_id=...&code_challenge=...
// GET /api/auth/callback?code=XXX&state=YYY → Exchange code for tokens
//   → POST {keycloak}/token with grant_type=authorization_code&code=XXX&code_verifier=...
```

**Frontend migration:**

```typescript
// CURRENT: Custom login form collects email + password → POST /api/auth/login
// FUTURE: Click "Login" → window.location.href = "/api/auth/login" → Keycloak login page → redirect back

// The custom login screen is REPLACED by Keycloak's login screen.
// This is the primary tradeoff: UX control vs security compliance.
```

**Token storage remains the same:** Access tokens in memory, refresh via backend. The Auth Code + PKCE flow doesn't change the frontend token storage strategy — it only changes HOW tokens are obtained.

**Timeline recommendation:** Document ROPC as a known limitation in project README. Plan migration as a standalone task when OAuth 2.1 becomes a formal RFC (expected 2025-2026). The backend abstraction layer (`IKeycloakTokenService`) makes the migration straightforward — only the implementation changes, not the interface.

---

## Verification Checklist

Before declaring Phase 9 complete, verify:

- [ ] No `localStorage`, `sessionStorage`, or any `window.*Storage` usage in auth code
- [ ] Tokens stored in module-level variables, NOT in React `useState`
- [ ] `noValidate` attribute present on login form
- [ ] `role="alert"` on error messages (LabeledField already provides this)
- [ ] `aria-invalid` and `aria-describedby` on input fields (LabeledField already provides this)
- [ ] Login form uses existing `LabeledField` + `AppButton` components
- [ ] Zod schema validates email format + password presence
- [ ] Generic error message shown for ALL login failures (no email enumeration)
- [ ] `isSubmitting` or `isLoading` disables the submit button during auth
- [ ] Protected routes redirect to `/login` when `isAuthenticated === false`
- [ ] AuthProvider wraps the app in the Vinxi entry point
- [ ] `useAuth()` throws if called outside `AuthProvider`
- [ ] Page refresh results in logout (tokens cleared from memory)
- [ ] No third-party HTTP client library installed (ky, axios) — native `fetch` only
- [ ] No `jwt-decode` or similar library — manual `atob` + `JSON.parse` for exp check
