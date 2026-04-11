import { createContext, useContext, useState, useEffect, type ReactNode } from "react";
import { loginClient, type LoginResponse } from "@/lib/api";

// ---------------------------------------------------------------------------
// Module-level token storage (SEC-10: memory only, NEVER localStorage)
// OUTSIDE any component — NOT in useState
// ---------------------------------------------------------------------------

interface AuthTokens {
  accessToken: string | null;
  expiresAt: number | null;
}

let tokens: AuthTokens = {
  accessToken: null,
  expiresAt: null,
};

// ---------------------------------------------------------------------------
// Context definition
// ---------------------------------------------------------------------------

interface AuthContextValue {
  auth: {
    isAuthenticated: boolean;
    isLoading: boolean;
  };
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshIfNeeded: () => Promise<void>;
  restoreSession: () => Promise<boolean>;
  getAccessToken: () => string | null;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

export function AuthProvider({ children }: { children: ReactNode }) {
  // Derived state only (booleans — OK for useState)
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true); // Start true for session restoration

  // Attempt to restore session on mount
  useEffect(() => {
    async function tryRestoreSession() {
      try {
        const response = await fetch("/api/auth/me", {
          method: "GET",
          credentials: "include", // Send httpOnly cookie
        });

        if (response.ok) {
          const data = await response.json() as { accessToken: string; expiresIn: number };
          tokens = {
            accessToken: data.accessToken,
            expiresAt: Date.now() + data.expiresIn * 1000,
          };
          setIsAuthenticated(true);
        }
      } catch {
        // Session restoration failed — user needs to login
      } finally {
        setIsLoading(false);
      }
    }

    tryRestoreSession();
  }, []);

  async function login(email: string, password: string): Promise<void> {
    setIsLoading(true);
    try {
      const response: LoginResponse = await loginClient(email, password);

      // Write to module-level variable — NOT state
      // Refresh token is now in httpOnly cookie (set by backend)
      tokens = {
        accessToken: response.accessToken,
        expiresAt: Date.now() + response.expiresIn * 1000,
      };

      setIsAuthenticated(true);
    } finally {
      setIsLoading(false);
    }
  }

  async function logout(): Promise<void> {
    // Call backend logout to clear httpOnly cookie
    try {
      await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "include",
      });
    } catch {
      // Cookie clear best-effort
    }

    // Reset module-level tokens
    tokens = {
      accessToken: null,
      expiresAt: null,
    };
    setIsAuthenticated(false);
  }

  async function restoreSession(): Promise<boolean> {
    try {
      const response = await fetch("/api/auth/me", {
        method: "GET",
        credentials: "include",
      });

      if (response.ok) {
        const data = await response.json() as { accessToken: string; expiresIn: number };
        tokens = {
          accessToken: data.accessToken,
          expiresAt: Date.now() + data.expiresIn * 1000,
        };
        setIsAuthenticated(true);
        return true;
      }
    } catch {
      // Session restoration failed
    }

    tokens = { accessToken: null, expiresAt: null };
    setIsAuthenticated(false);
    return false;
  }

  async function refreshIfNeeded(): Promise<void> {
    if (!tokens.expiresAt) return;

    const timeUntilExpiry = tokens.expiresAt - Date.now();
    if (timeUntilExpiry > 60_000) return; // More than 60s — no refresh needed

    try {
      const response = await fetch("/api/auth/refresh", {
        method: "POST",
        credentials: "include", // Send httpOnly cookie
      });

      if (response.ok) {
        const data = await response.json() as { accessToken: string; expiresIn: number };
        tokens = {
          accessToken: data.accessToken,
          expiresAt: Date.now() + data.expiresIn * 1000,
        };
      } else {
        // Refresh failed — user must re-login
        await logout();
      }
    } catch {
      // Refresh failed — user must re-login
      await logout();
    }
  }

  function getAccessToken(): string | null {
    return tokens.accessToken;
  }

  return (
    <AuthContext.Provider
      value={{
        auth: { isAuthenticated, isLoading },
        login,
        logout,
        refreshIfNeeded,
        restoreSession,
        getAccessToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

// ---------------------------------------------------------------------------
// Standalone token accessor (for use outside React components)
// Used by API clients that cannot use hooks — avoids circular dependency
// via dynamic import().
// ---------------------------------------------------------------------------

export function getAccessToken(): string | null {
  return tokens.accessToken;
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
