import {
  createRouter,
  defineEventHandler,
  getQuery,
  setCookie,
  deleteCookie,
  getCookie,
  getRequestHeader,
  sendRedirect,
} from "h3";
import {
  generateCodeVerifier,
  generateCodeChallenge,
  buildAuthorizationUrl,
  exchangeCodeForTokens,
  refreshAccessToken,
} from "./src/lib/auth-code-flow";

const router = createRouter();

// ── Config ──────────────────────────────────────────────────────────────
const KEYCLOAK_URL = process.env.KEYCLOAK_URL || "http://localhost:8180";
const KEYCLOAK_PUBLIC_URL = process.env.KEYCLOAK_PUBLIC_URL || "http://localhost:8180";

// Realm resolution — fail-fast strategy (D-13: compose sets KEYCLOAK_REALM=backoffice).
// Per-SPA default "backoffice" keeps `pnpm dev` working on a fresh checkout without .env.
// The legacy value "onboarding" was the root cause of Bug 1 (realm does not exist).
// Supported realms: "client", "backoffice". Any other value is a misconfiguration.
(function validateRealm() {
  const raw = process.env.KEYCLOAK_REALM;
  if (raw !== undefined && raw !== "client" && raw !== "backoffice") {
    throw new Error(
      `[auth-server/backoffice] KEYCLOAK_REALM="${raw}" is not a supported realm. ` +
        `Supported values: "client", "backoffice". ` +
        `The legacy realm "onboarding" no longer exists (removed in Phase 34). ` +
        `Set KEYCLOAK_REALM=backoffice for the backoffice SPA.`
    );
  }
})();
const KEYCLOAK_REALM = process.env.KEYCLOAK_REALM ?? "backoffice";
const CLIENT_ID = process.env.KEYCLOAK_CLIENT_ID || "onboarding-backoffice";
const CLIENT_SECRET = process.env.KEYCLOAK_CLIENT_SECRET || "";
const FRONTEND_URL = process.env.FRONTEND_URL || "http://localhost:5174";
const REDIRECT_URI = `${FRONTEND_URL}/auth/callback`;
const IS_PROD = process.env.NODE_ENV === "production";

// ── GET /auth/login — Redirect to Keycloak authorization URL ────────────
router.get(
  "/login",
  defineEventHandler(async (event) => {
    const codeVerifier = generateCodeVerifier();
    const codeChallenge = await generateCodeChallenge(codeVerifier);
    const state = generateCodeVerifier().slice(0, 20);

    // Defensive: clear stale PKCE cookies before setting new ones
    // (prevents Brave/privacy-browser issues with leftover cookies)
    deleteCookie(event, "pkce_code_verifier", { path: "/auth" });
    deleteCookie(event, "pkce_code_verifier", { path: "/" });
    deleteCookie(event, "pkce_state", { path: "/auth" });
    deleteCookie(event, "pkce_state", { path: "/" });

    setCookie(event, "pkce_code_verifier", codeVerifier, {
      httpOnly: true,
      secure: IS_PROD,
      sameSite: "lax",
      path: "/auth",
      maxAge: 600,
    });
    setCookie(event, "pkce_state", state, {
      httpOnly: true,
      secure: IS_PROD,
      sameSite: "lax",
      path: "/auth",
      maxAge: 600,
    });

    // If this login was triggered by a PKCE cookie retry, forward the retry param
    // in the redirect URI so it arrives back in the callback query string.
    // This is the fallback when the browser blocks even the pkce_retry cookie.
    const retryQuery = getQuery(event).retry as string | undefined;
    const callbackUri = retryQuery === "1" ? `${REDIRECT_URI}?retry=1` : REDIRECT_URI;

    const authUrl = buildAuthorizationUrl({
      keycloakUrl: KEYCLOAK_PUBLIC_URL,
      realm: KEYCLOAK_REALM,
      clientId: CLIENT_ID,
      redirectUri: callbackUri,
      codeChallenge,
      state,
    });

    return sendRedirect(event, authUrl, 302);
  })
);

// ── GET /auth/callback — Exchange code for tokens ──────────────────────
router.get(
  "/callback",
  defineEventHandler(async (event) => {
    const query = getQuery(event);
    const code = query.code as string | undefined;
    const state = query.state as string | undefined;
    const error = query.error as string | undefined;

    if (error) {
      const errorDesc = (query.error_description as string) || "Authentication failed";
      return sendRedirect(
        event,
        `/auth/error?error=${encodeURIComponent(errorDesc)}`,
        302
      );
    }

    if (!code || !state) {
      return sendRedirect(event, "/auth/error?error=Missing+code+or+state", 302);
    }

    const storedState = getCookie(event, "pkce_state");
    const codeVerifier = getCookie(event, "pkce_code_verifier");
    // Retry tracking: cookie is primary, URL query param is fallback (if cookies completely blocked)
    const retryFromCookie = parseInt(getCookie(event, "pkce_retry") || "0", 10);
    const retryFromQuery = parseInt((query.retry as string) || "0", 10);
    const retryCount = Math.max(retryFromCookie, retryFromQuery);

    if (state !== storedState || !codeVerifier) {
      // Diagnostic logging — captures evidence for Brave/privacy-browser cookie issues
      const userAgent = getRequestHeader(event, "user-agent") || "unknown";
      const cookieHeader = getRequestHeader(event, "cookie") || "(empty)";
      console.warn(
        `[auth/callback] PKCE cookie mismatch — ` +
          `query.state=${state}, cookie.pkce_state=${storedState ?? "(missing)"}, ` +
          `code_verifier=${codeVerifier ? "present" : "(missing)"}, ` +
          `retry=${retryCount}, ` +
          `cookies=[${cookieHeader.replace(/=[^;]+/g, "=***")}], ` +
          `ua=${userAgent}`
      );

      // Auto-retry: redirect to /auth/login to restart the flow.
      // Keycloak already has an active session → user won't re-enter credentials.
      // Max 1 retry to prevent infinite loops.
      if (retryCount < 1) {
        setCookie(event, "pkce_retry", "1", {
          httpOnly: true,
          secure: IS_PROD,
          sameSite: "lax",
          path: "/",
          maxAge: 120,
        });
        return sendRedirect(event, "/auth/login?retry=1", 302);
      }

      // Retry exhausted — clear retry cookie, show error with guidance
      deleteCookie(event, "pkce_retry", { path: "/" });
      return sendRedirect(
        event,
        `/auth/error?error=${encodeURIComponent(
          "Cookies de sessão não foram preservados pelo navegador. " +
            "Tente desativar bloqueadores de cookies/Shields para localhost, " +
            "ou use Chrome/Firefox."
        )}`,
        302
      );
    }

    try {
      // redirect_uri in token exchange must match the one sent to the authorize endpoint
      const exchangeRedirectUri = retryFromQuery > 0 ? `${REDIRECT_URI}?retry=1` : REDIRECT_URI;

      const tokens = await exchangeCodeForTokens({
        keycloakUrl: KEYCLOAK_URL,
        realm: KEYCLOAK_REALM,
        clientId: CLIENT_ID,
        clientSecret: CLIENT_SECRET,
        code,
        codeVerifier,
        redirectUri: exchangeRedirectUri,
      });

      // Set httpOnly cookies for tokens
      // sameSite="lax": allows cookie on top-level navigation after KC:8180→SPA:5174 302 chain,
      // fixing the cookie-not-sent race (Bug 2 hypothesis 2). CSRF still covered by PKCE state
      // validation on the callback and lax blocking cross-site subresource/form POSTs. (D-15)
      setCookie(event, "backoffice_access_token", tokens.accessToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "lax",
        path: "/",
        maxAge: tokens.expiresIn || 300,
      });
      // sameSite="strict": refresh token is only sent from same-origin fetch (/auth/refresh),
      // never needs to ride a cross-site top-level navigation, so strict is safe and tighter.
      setCookie(event, "backoffice_refresh_token", tokens.refreshToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: 28800,
      });
      // id_token stored server-side only (T-18 / W-SEC-IT4-1). Used exclusively as
      // id_token_hint on logout so Keycloak can skip the confirmation page. Never exposed
      // to browser JS. D-12: id_token never in localStorage / sessionStorage / JS-readable
      // cookie. maxAge aligned with access token TTL — the hint is only meaningful for the
      // same session; a fresh login will overwrite this cookie.
      if (tokens.idToken) {
        setCookie(event, "backoffice_id_token", tokens.idToken, {
          httpOnly: true,
          secure: IS_PROD,
          sameSite: "lax",
          path: "/",
          maxAge: tokens.expiresIn || 300,
        });
      }

      // Clean up PKCE cookies + retry cookie
      deleteCookie(event, "pkce_code_verifier", { path: "/auth" });
      deleteCookie(event, "pkce_state", { path: "/auth" });
      deleteCookie(event, "pkce_retry", { path: "/" });

      // Detect first login via isFirstLogin claim in access token
      let isFirstLogin = false;
      try {
        const parts = tokens.accessToken.split(".");
        if (parts.length >= 2) {
          const payload = JSON.parse(
            Buffer.from(parts[1], "base64").toString("utf-8")
          ) as Record<string, unknown>;
          isFirstLogin =
            payload.isFirstLogin === "true" || payload.isFirstLogin === true;
        }
      } catch {
        // Token invalid/undecodable → treat as non-first-login (normal flow)
        isFirstLogin = false;
      }

      if (isFirstLogin) {
        // Best-effort: call backend to clear the flag.
        // Even if it fails, we proceed with cookie cleanup + redirect to /admin/login
        // (self-healing: next login repeats the cycle if the flag wasn't cleared).
        const backendUrl = "http://api:8080";
        try {
          await fetch(`${backendUrl}/api/admin/me/complete-first-login`, {
            method: "POST",
            headers: {
              Authorization: `Bearer ${tokens.accessToken}`,
              "Content-Type": "application/json",
            },
          });
        } catch (err) {
          // Log only — don't block the redirect
          console.error(
            "[auth/callback] Failed to clear isFirstLogin flag:",
            err
          );
        }

        // Clear session cookies to force re-login with the new password.
        // Also delete the id_token cookie so the stale hint does not reach
        // the next logout handler (T-18: id_token TTL matches access token).
        deleteCookie(event, "backoffice_access_token", { path: "/" });
        deleteCookie(event, "backoffice_refresh_token", { path: "/" });
        deleteCookie(event, "backoffice_id_token", { path: "/" });

        return sendRedirect(event, "/admin/login", 302);
      }

      return sendRedirect(event, "/admin/companies", 302);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Token exchange failed";
      return sendRedirect(
        event,
        `/auth/error?error=${encodeURIComponent(message)}`,
        302
      );
    }
  })
);

// ── GET /auth/logout — Clear cookies + redirect to Keycloak OIDC logout ─
router.get(
  "/logout",
  defineEventHandler(async (event) => {
    // Read id_token BEFORE deleting cookies (T-18 / W-SEC-IT4-1).
    // id_token_hint allows Keycloak to skip the "Do you want to log out?"
    // confirmation page and redirect straight to post_logout_redirect_uri.
    // If absent (e.g. older session predating T-18), we fall back gracefully
    // to the client_id-only path — D-15 invariant is met in both cases.
    const idToken = getCookie(event, "backoffice_id_token");

    deleteCookie(event, "backoffice_access_token", { path: "/" });
    deleteCookie(event, "backoffice_refresh_token", { path: "/" });
    // Delete id_token cookie alongside access/refresh (D-12: no token persists post-logout).
    deleteCookie(event, "backoffice_id_token", { path: "/" });

    const logoutUrl = `${KEYCLOAK_PUBLIC_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/logout`;
    const postLogoutRedirectUri = `${FRONTEND_URL}/auth/login`;
    let fullUrl = `${logoutUrl}?post_logout_redirect_uri=${encodeURIComponent(postLogoutRedirectUri)}&client_id=${encodeURIComponent(CLIENT_ID)}`;
    // T-18: append id_token_hint when available so KC skips confirmation page (D-15 strengthened).
    // Graceful fallback: omit parameter entirely when cookie absent — do NOT hard-fail.
    if (idToken) {
      fullUrl += `&id_token_hint=${encodeURIComponent(idToken)}`;
    }

    return sendRedirect(event, fullUrl, 302);
  })
);

// ── GET /auth/me — Return admin info from access token ──────────────────
router.get(
  "/me",
  defineEventHandler(async (event) => {
    const accessToken = getCookie(event, "backoffice_access_token");
    if (!accessToken) {
      event.node.res.statusCode = 401;
      return { isAuthenticated: false };
    }

    try {
      // Decode JWT payload (base64url) — no signature verification needed
      // since token came from our own server-side exchange
      const parts = accessToken.split(".");
      if (parts.length < 2) throw new Error("Invalid token");
      const payload = JSON.parse(
        Buffer.from(parts[1], "base64").toString("utf-8")
      ) as Record<string, unknown>;

      const sub = (payload.sub as string) || "";
      const email = (payload.email as string) || "";
      const name =
        (payload.name as string) ||
        (payload.preferred_username as string) ||
        email.split("@")[0] ||
        "Admin";

      return {
        isAuthenticated: true,
        adminName: name,
        email,
        sub,
      };
    } catch {
      event.node.res.statusCode = 401;
      return { isAuthenticated: false };
    }
  })
);

// ── POST /auth/refresh — Refresh access token ──────────────────────────
router.post(
  "/refresh",
  defineEventHandler(async (event) => {
    const refreshToken = getCookie(event, "backoffice_refresh_token");
    if (!refreshToken) {
      event.node.res.statusCode = 401;
      return { error: "No refresh token" };
    }

    try {
      const tokens = await refreshAccessToken({
        keycloakUrl: KEYCLOAK_URL,
        realm: KEYCLOAK_REALM,
        clientId: CLIENT_ID,
        clientSecret: CLIENT_SECRET,
        refreshToken,
      });

      // sameSite="lax" on access token — consistent with /auth/callback (see comment above).
      setCookie(event, "backoffice_access_token", tokens.accessToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "lax",
        path: "/",
        maxAge: tokens.expiresIn || 300,
      });
      if (tokens.refreshToken !== refreshToken) {
        // sameSite="strict" on refresh token — same-origin /auth/refresh only, no navigation needed.
        setCookie(event, "backoffice_refresh_token", tokens.refreshToken, {
          httpOnly: true,
          secure: IS_PROD,
          sameSite: "strict",
          path: "/",
          maxAge: 28800,
        });
      }

      return { ok: true };
    } catch (err) {
      const message = err instanceof Error ? err.message : "Refresh failed";
      event.node.res.statusCode = 401;
      return { error: message };
    }
  })
);

export default router.handler;
