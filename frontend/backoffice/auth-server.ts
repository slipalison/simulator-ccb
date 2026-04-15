// ---------------------------------------------------------------------------
// Auth Server — Vinxi h3 handler for Authorization Code Flow + PKCE
// ---------------------------------------------------------------------------
// Routes (all relative to /auth base):
//   GET /login     — generate PKCE, set cookies, redirect to Keycloak
//   GET /callback  — exchange code for tokens, set httpOnly cookies, redirect to /admin/users
//   GET /logout    — clear cookies, redirect to Keycloak OIDC logout
//   GET /me        — decode access_token cookie, return user info
//   POST /refresh  — exchange refresh_token for new tokens
// ---------------------------------------------------------------------------

import { defineEventHandler, getCookie, setCookie, deleteCookie, sendRedirect, getQuery } from "h3";
import {
  generateCodeVerifier,
  generateCodeChallenge,
  buildAuthorizationUrl,
  exchangeCodeForTokens,
  refreshAccessToken,
  decodeJwtPayload,
} from "./src/lib/auth-code-flow";

const KEYCLOAK_EXTERNAL_URL =
  process.env.KEYCLOAK_EXTERNAL_URL ?? "http://localhost:8180";
const KEYCLOAK_INTERNAL_URL =
  process.env.KEYCLOAK_INTERNAL_URL ?? "http://keycloak:8080";
const KEYCLOAK_REALM = process.env.KEYCLOAK_REALM ?? "onboarding";
const CLIENT_ID =
  process.env.KEYCLOAK_BACKOFFICE_CLIENT_ID ?? "onboarding-backoffice";
const CLIENT_SECRET = process.env.KEYCLOAK_BACKOFFICE_CLIENT_SECRET ?? "";
const REDIRECT_URI =
  process.env.BACKOFFICE_REDIRECT_URI ?? "http://localhost:5174/auth/callback";
const POST_LOGOUT_REDIRECT_URI =
  process.env.BACKOFFICE_POST_LOGOUT_REDIRECT_URI ??
  "http://localhost:5174/auth/login";

const COOKIE_OPTS = {
  httpOnly: true,
  secure: false, // set true in production
  sameSite: "strict" as const,
  path: "/",
};

export default defineEventHandler(async (event) => {
  const rawPath = event.path ?? "/";
  const pathname = rawPath.split("?")[0];
  const method = event.node.req.method ?? "GET";

  // ---------------------------------------------------------------------------
  // GET /login — generate PKCE and redirect to Keycloak authorization endpoint
  // ---------------------------------------------------------------------------
  if (pathname === "/login" && method === "GET") {
    const codeVerifier = generateCodeVerifier();
    const codeChallenge = generateCodeChallenge(codeVerifier);
    const state = generateCodeVerifier(); // reuse for random state

    setCookie(event, "pkce_verifier", codeVerifier, {
      ...COOKIE_OPTS,
      maxAge: 300,
    });
    setCookie(event, "oauth_state", state, {
      ...COOKIE_OPTS,
      maxAge: 300,
    });

    const authUrl = buildAuthorizationUrl({
      clientId: CLIENT_ID,
      redirectUri: REDIRECT_URI,
      keycloakUrl: KEYCLOAK_EXTERNAL_URL,
      realm: KEYCLOAK_REALM,
      state,
      codeChallenge,
    });

    return sendRedirect(event, authUrl, 302);
  }

  // ---------------------------------------------------------------------------
  // GET /callback — exchange authorization code for tokens, set cookies
  // ---------------------------------------------------------------------------
  if (pathname === "/callback" && method === "GET") {
    const query = getQuery(event) as Record<string, string>;
    const code = query.code;
    const state = query.state;

    const expectedState = getCookie(event, "oauth_state");
    const codeVerifier = getCookie(event, "pkce_verifier");

    if (
      !code ||
      !state ||
      !expectedState ||
      state !== expectedState ||
      !codeVerifier
    ) {
      return sendRedirect(event, "/auth/error?reason=invalid_state", 302);
    }

    try {
      const tokens = await exchangeCodeForTokens({
        code,
        codeVerifier,
        clientId: CLIENT_ID,
        clientSecret: CLIENT_SECRET,
        redirectUri: REDIRECT_URI,
        keycloakUrl: KEYCLOAK_INTERNAL_URL,
        realm: KEYCLOAK_REALM,
      });

      // Clear PKCE cookies
      deleteCookie(event, "pkce_verifier", { path: "/" });
      deleteCookie(event, "oauth_state", { path: "/" });

      // Set token cookies (httpOnly — not visible to JS)
      setCookie(event, "backoffice_access_token", tokens.access_token, {
        ...COOKIE_OPTS,
        maxAge: tokens.expires_in,
      });
      setCookie(event, "backoffice_refresh_token", tokens.refresh_token, {
        ...COOKIE_OPTS,
        maxAge: tokens.refresh_expires_in,
      });
      if (tokens.id_token) {
        setCookie(event, "backoffice_id_token", tokens.id_token, {
          ...COOKIE_OPTS,
          maxAge: tokens.expires_in,
        });
      }

      return sendRedirect(event, "/admin/users", 302);
    } catch {
      return sendRedirect(
        event,
        "/auth/error?reason=token_exchange_failed",
        302
      );
    }
  }

  // ---------------------------------------------------------------------------
  // GET /logout — clear cookies and redirect to Keycloak OIDC logout
  // ---------------------------------------------------------------------------
  if (pathname === "/logout" && method === "GET") {
    const idToken = getCookie(event, "backoffice_id_token");

    deleteCookie(event, "backoffice_access_token", { path: "/" });
    deleteCookie(event, "backoffice_refresh_token", { path: "/" });
    deleteCookie(event, "backoffice_id_token", { path: "/" });

    const logoutUrl = new URL(
      `${KEYCLOAK_EXTERNAL_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/logout`
    );
    if (idToken) {
      logoutUrl.searchParams.set("id_token_hint", idToken);
    }
    logoutUrl.searchParams.set(
      "post_logout_redirect_uri",
      POST_LOGOUT_REDIRECT_URI
    );
    logoutUrl.searchParams.set("client_id", CLIENT_ID);

    return sendRedirect(event, logoutUrl.toString(), 302);
  }

  // ---------------------------------------------------------------------------
  // GET /me — decode access_token cookie and return user info
  // ---------------------------------------------------------------------------
  if (pathname === "/me" && method === "GET") {
    const accessToken = getCookie(event, "backoffice_access_token");

    if (!accessToken) {
      event.node.res.statusCode = 401;
      return { error: "Unauthorized" };
    }

    try {
      const payload = decodeJwtPayload(accessToken);
      const roles =
        (payload.realm_access as { roles?: string[] })?.roles ?? [];

      event.node.res.statusCode = 200;
      return {
        adminEmail: payload.email as string,
        adminName: ((payload.name ?? payload.preferred_username) as string),
        roles,
      };
    } catch {
      event.node.res.statusCode = 401;
      return { error: "Invalid token" };
    }
  }

  // ---------------------------------------------------------------------------
  // POST /refresh — exchange refresh_token for new tokens
  // ---------------------------------------------------------------------------
  if (pathname === "/refresh" && method === "POST") {
    const refreshToken = getCookie(event, "backoffice_refresh_token");

    if (!refreshToken) {
      event.node.res.statusCode = 401;
      return { error: "No refresh token" };
    }

    try {
      const tokens = await refreshAccessToken({
        refreshToken,
        clientId: CLIENT_ID,
        clientSecret: CLIENT_SECRET,
        keycloakUrl: KEYCLOAK_INTERNAL_URL,
        realm: KEYCLOAK_REALM,
      });

      setCookie(event, "backoffice_access_token", tokens.access_token, {
        ...COOKIE_OPTS,
        maxAge: tokens.expires_in,
      });
      setCookie(event, "backoffice_refresh_token", tokens.refresh_token, {
        ...COOKIE_OPTS,
        maxAge: tokens.refresh_expires_in,
      });
      if (tokens.id_token) {
        setCookie(event, "backoffice_id_token", tokens.id_token, {
          ...COOKIE_OPTS,
          maxAge: tokens.expires_in,
        });
      }

      event.node.res.statusCode = 204;
      return null;
    } catch {
      event.node.res.statusCode = 401;
      return { error: "Token refresh failed" };
    }
  }

  event.node.res.statusCode = 404;
  return { error: "Not found" };
});
