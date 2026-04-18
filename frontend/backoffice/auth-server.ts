import {
  createRouter,
  defineEventHandler,
  getQuery,
  setCookie,
  deleteCookie,
  getCookie,
  sendRedirect,
  readBody,
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
const KEYCLOAK_REALM = process.env.KEYCLOAK_REALM || "onboarding";
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

    const authUrl = buildAuthorizationUrl({
      keycloakUrl: KEYCLOAK_PUBLIC_URL,
      realm: KEYCLOAK_REALM,
      clientId: CLIENT_ID,
      redirectUri: REDIRECT_URI,
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
    if (state !== storedState) {
      return sendRedirect(event, "/auth/error?error=Invalid+state", 302);
    }

    const codeVerifier = getCookie(event, "pkce_code_verifier");
    if (!codeVerifier) {
      return sendRedirect(event, "/auth/error?error=Missing+code+verifier", 302);
    }

    try {
      const tokens = await exchangeCodeForTokens({
        keycloakUrl: KEYCLOAK_URL,
        realm: KEYCLOAK_REALM,
        clientId: CLIENT_ID,
        clientSecret: CLIENT_SECRET,
        code,
        codeVerifier,
        redirectUri: REDIRECT_URI,
      });

      // Set httpOnly cookies for tokens
      setCookie(event, "backoffice_access_token", tokens.accessToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: tokens.expiresIn || 300,
      });
      setCookie(event, "backoffice_refresh_token", tokens.refreshToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: 28800,
      });

      // Clean up PKCE cookies
      deleteCookie(event, "pkce_code_verifier", { path: "/auth" });
      deleteCookie(event, "pkce_state", { path: "/auth" });

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

        // Clear session cookies to force re-login with the new password
        deleteCookie(event, "backoffice_access_token", { path: "/" });
        deleteCookie(event, "backoffice_refresh_token", { path: "/" });

        return sendRedirect(event, "/admin/login", 302);
      }

      return sendRedirect(event, "/admin/users", 302);
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
    deleteCookie(event, "backoffice_access_token", { path: "/" });
    deleteCookie(event, "backoffice_refresh_token", { path: "/" });

    const logoutUrl = `${KEYCLOAK_PUBLIC_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/logout`;
    const postLogoutRedirectUri = `${FRONTEND_URL}/auth/login`;
    const fullUrl = `${logoutUrl}?post_logout_redirect_uri=${encodeURIComponent(postLogoutRedirectUri)}&client_id=${encodeURIComponent(CLIENT_ID)}`;

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

      setCookie(event, "backoffice_access_token", tokens.accessToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: tokens.expiresIn || 300,
      });
      if (tokens.refreshToken !== refreshToken) {
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
