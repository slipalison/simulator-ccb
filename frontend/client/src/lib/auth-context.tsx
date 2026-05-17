import { createContext, useContext, useState, useEffect, type ReactNode } from "react";

// ---------------------------------------------------------------------------
// Access group type — matches Keycloak group names (Phase 39)
// ---------------------------------------------------------------------------

export type AccessGroup = "admin-empresa" | "viewer" | "dashboard";

// ---------------------------------------------------------------------------
// Group-based default routes per D-22
// ---------------------------------------------------------------------------

const GROUP_DEFAULT_ROUTES: Record<AccessGroup, string> = {
  "admin-empresa": "/employees",
  viewer: "/employees",
  dashboard: "/dashboard",
};

/** Returns the default route for a given access group. Fallback: /profile */
export function getDefaultRouteForGroup(group: AccessGroup | null): string {
  return group ? (GROUP_DEFAULT_ROUTES[group] ?? "/profile") : "/profile";
}

// ---------------------------------------------------------------------------
// Context definition
// ---------------------------------------------------------------------------

interface AuthContextValue {
  auth: {
    isAuthenticated: boolean;
    isLoading: boolean;
    userName: string | null;
    email: string | null;
    accessGroup: AccessGroup | null;
    companyId: string | null;
    permissions: string[];
  };
  /** Redirects to /auth/login (Vinxi server → Keycloak ACF) */
  login: () => void;
  /** Redirects to /auth/logout (Vinxi server clears cookies → Keycloak OIDC logout) */
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [userName, setUserName] = useState<string | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [accessGroup, setAccessGroup] = useState<AccessGroup | null>(null);
  const [companyId, setCompanyId] = useState<string | null>(null);
  const [permissions, setPermissions] = useState<string[]>([]);

  // Session restoration on mount via /auth/me
  // If /auth/me fails with 401, try /auth/refresh first (access token
  // may be expired but refresh token is still valid), then retry /auth/me.
  useEffect(() => {
    async function tryRestore() {
      try {
        let res = await fetch("/auth/me", { credentials: "include" });

        if (res.status === 401) {
          const refreshRes = await fetch("/auth/refresh", {
            method: "POST",
            credentials: "include",
          });
          if (refreshRes.ok) {
            res = await fetch("/auth/me", { credentials: "include" });
          }
        }

        if (res.ok) {
          const data = (await res.json()) as {
            userName: string;
            email: string;
            isAuthenticated: boolean;
            accessGroup?: string | null;
            companyId?: string | null;
            permissions?: string[] | null;
          };
          setUserName(data.userName);
          setEmail(data.email);
          setIsAuthenticated(data.isAuthenticated);
          setAccessGroup(
            data.accessGroup
              ? (data.accessGroup as AccessGroup)
              : null
          );
          setCompanyId(data.companyId ?? null);
          setPermissions(data.permissions ?? []);
        }
      } catch {
        // Session invalid — user needs to login
      } finally {
        setIsLoading(false);
      }
    }

    tryRestore();
  }, []);

  function login(): void {
    window.location.href = "/auth/login";
  }

  function logout(): void {
    window.location.href = "/auth/logout";
  }

  return (
    <AuthContext.Provider
      value={{
        auth: { isAuthenticated, isLoading, userName, email, accessGroup, companyId, permissions },
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

// ---------------------------------------------------------------------------
// Hook
// ---------------------------------------------------------------------------

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
