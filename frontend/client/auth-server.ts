import {
  createRouter,
  defineEventHandler,
  getQuery,
  setCookie,
  deleteCookie,
  getCookie,
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
const KEYCLOAK_REALM = process.env.KEYCLOAK_REALM || "onboarding";
const CLIENT_ID = process.env.KEYCLOAK_CLIENT_ACF_CLIENT_ID || "onboarding-client-acf";
const CLIENT_SECRET = process.env.KEYCLOAK_CLIENT_ACF_CLIENT_SECRET || "";
const FRONTEND_URL = process.env.FRONTEND_URL || "http://localhost:5173";
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

      // Set httpOnly cookies for tokens (client-specific names to avoid conflict with backoffice)
      setCookie(event, "client_access_token", tokens.accessToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: tokens.expiresIn || 300,
      });
      setCookie(event, "client_refresh_token", tokens.refreshToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: 28800,
      });

      // Clean up PKCE cookies
      deleteCookie(event, "pkce_code_verifier", { path: "/auth" });
      deleteCookie(event, "pkce_state", { path: "/auth" });

      return sendRedirect(event, "/profile", 302);
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
    deleteCookie(event, "client_access_token", { path: "/" });
    deleteCookie(event, "client_refresh_token", { path: "/" });

    const logoutUrl = `${KEYCLOAK_PUBLIC_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/logout`;
    const postLogoutRedirectUri = `${FRONTEND_URL}/auth/login`;
    const fullUrl = `${logoutUrl}?post_logout_redirect_uri=${encodeURIComponent(postLogoutRedirectUri)}`;

    return sendRedirect(event, fullUrl, 302);
  })
);

// ── GET /auth/me — Return user info from access token ──────────────────
router.get(
  "/me",
  defineEventHandler(async (event) => {
    const accessToken = getCookie(event, "client_access_token");
    if (!accessToken) {
      event.node.res.statusCode = 401;
      return { isAuthenticated: false };
    }

    try {
      const parts = accessToken.split(".");
      if (parts.length < 2) throw new Error("Invalid token");
      const payload = JSON.parse(
        Buffer.from(parts[1], "base64").toString("utf-8")
      ) as Record<string, unknown>;

      // Extract claims from JWT; fall back to UserInfo/Admin API if claims are missing
      let sub = (payload.sub as string) || "";
      let email = (payload.email as string) || "";
      let name =
        (payload.name as string) ||
        (payload.preferred_username as string) ||
        email.split("@")[0] ||
        "User";
      let groups = payload.groups as string[] | undefined;

      // If sub or email are missing from the access token, fetch from UserInfo endpoint
      if (!sub || !email || !groups) {
        try {
          const userInfoUrl = `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/userinfo`;
          const userInfoRes = await fetch(userInfoUrl, {
            headers: { Authorization: `Bearer ${accessToken}` },
          });
          if (userInfoRes.ok) {
            const userInfo = (await userInfoRes.json()) as Record<string, unknown>;
            if (!sub) sub = (userInfo.sub as string) || "";
            if (!email) email = (userInfo.email as string) || "";
            // Use the UUID sub from UserInfo if available (Keycloak returns UUID as sub)
            if (userInfo.sub && typeof userInfo.sub === "string" && userInfo.sub.includes("-")) {
              sub = userInfo.sub;
            }
            if (name === "User" || !name) {
              name = (userInfo.name as string) ||
                (userInfo.preferred_username as string) ||
                email.split("@")[0] ||
                "User";
            }
          }
        } catch {
          // UserInfo fetch failed — continue with JWT claims only
        }
      }

      // If groups still missing, fetch user groups and email via Keycloak Admin API
      if ((!groups || !email) && sub) {
        try {
          // Get service account token for admin API access
          const adminTokenUrl = `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`;
          const adminTokenRes = await fetch(adminTokenUrl, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: "grant_type=client_credentials&client_id=onboarding-api-admin&client_secret=" + encodeURIComponent(CLIENT_SECRET),
          });
          if (adminTokenRes.ok) {
            const adminTokenData = (await adminTokenRes.json()) as { access_token: string };
            const adminToken = adminTokenData.access_token;

            // First, resolve sub to UUID if it looks like an email (Keycloak Admin API needs UUID)
            let userId = sub;
            if (sub.includes("@")) {
              const searchUrl = `${KEYCLOAK_URL}/admin/realms/${KEYCLOAK_REALM}/users?email=${encodeURIComponent(sub)}&exact=true`;
              const searchRes = await fetch(searchUrl, {
                headers: { Authorization: `Bearer ${adminToken}` },
              });
              if (searchRes.ok) {
                const users = (await searchRes.json()) as Array<{ id: string }>;
                if (users.length > 0) {
                  userId = users[0].id;
                  sub = userId; // Update sub to UUID
                }
              }
            }

            // Fetch user groups
            if (!groups) {
              const userGroupsUrl = `${KEYCLOAK_URL}/admin/realms/${KEYCLOAK_REALM}/users/${encodeURIComponent(userId)}/groups`;
              const groupsRes = await fetch(userGroupsUrl, {
                headers: { Authorization: `Bearer ${adminToken}` },
              });
            if (groupsRes.ok) {
              const userGroups = (await groupsRes.json()) as Array<{ name: string }>;
              groups = userGroups.map((g) => g.name);
            }
          }

            // Fetch user email
            if (!email) {
              const userUrl = `${KEYCLOAK_URL}/admin/realms/${KEYCLOAK_REALM}/users/${encodeURIComponent(userId)}`;
              const userRes = await fetch(userUrl, {
                headers: { Authorization: `Bearer ${adminToken}` },
              });
              if (userRes.ok) {
                const userData = (await userRes.json()) as { email?: string };
                if (userData.email) {
                  email = userData.email;
                }
              }
            }
          }
        } catch {
          // Admin API fetch failed — continue with JWT claims only
        }
      }

      const realmRoles = (payload.realm_access as Record<string, unknown>)?.roles as string[] | undefined;
      // Merge groups from API with groups from token (token groups take precedence if present)

      let accessGroup: string | null = null;
      if (groups?.includes("admin-empresa")) {
        accessGroup = "admin-empresa";
      } else if (groups?.includes("viewer")) {
        accessGroup = "viewer";
      } else if (groups?.includes("dashboard")) {
        accessGroup = "dashboard";
      } else if (realmRoles?.includes("admin-empresa")) {
        accessGroup = "admin-empresa";
      } else if (realmRoles?.includes("viewer")) {
        accessGroup = "viewer";
      } else if (realmRoles?.includes("dashboard")) {
        accessGroup = "dashboard";
      }

      const companyId = (payload.company_id as string) || (payload["custom:company_id"] as string) || null;

      return {
        isAuthenticated: true,
        userName: name,
        email,
        sub,
        accessGroup,
        companyId,
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
    const refreshToken = getCookie(event, "client_refresh_token");
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

      setCookie(event, "client_access_token", tokens.accessToken, {
        httpOnly: true,
        secure: IS_PROD,
        sameSite: "strict",
        path: "/",
        maxAge: tokens.expiresIn || 300,
      });
      if (tokens.refreshToken !== refreshToken) {
        setCookie(event, "client_refresh_token", tokens.refreshToken, {
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
