import { createContext, useContext, useState, type ReactNode } from "react";
import { loginClient, refreshTokenClient, type LoginResponse } from "@/lib/api";

// ---------------------------------------------------------------------------
// Module-level token storage (SEC-10: memory only, NEVER localStorage)
// OUTSIDE any component — NOT in useState
// ---------------------------------------------------------------------------

interface AuthTokens {
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: number | null;
  refreshExpiresAt: number | null;
}

let tokens: AuthTokens = {
  accessToken: null,
  refreshToken: null,
  expiresAt: null,
  refreshExpiresAt: null,
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
  logout: () => void;
  refreshIfNeeded: () => Promise<void>;
  getAccessToken: () => string | null;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

export function AuthProvider({ children }: { children: ReactNode }) {
  // Derived state only (booleans — OK for useState)
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  async function login(email: string, password: string): Promise<void> {
    setIsLoading(true);
    try {
      const response: LoginResponse = await loginClient(email, password);

      // Write to module-level variable — NOT state
      tokens = {
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        expiresAt: Date.now() + response.expiresIn * 1000,
        refreshExpiresAt: Date.now() + response.refreshExpiresIn * 1000,
      };

      setIsAuthenticated(true);
    } finally {
      setIsLoading(false);
    }
  }

  function logout(): void {
    // Reset module-level tokens
    tokens = {
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      refreshExpiresAt: null,
    };
    setIsAuthenticated(false);
  }

  async function refreshIfNeeded(): Promise<void> {
    if (!tokens.refreshToken) return;
    if (!tokens.expiresAt) return;

    const timeUntilExpiry = tokens.expiresAt - Date.now();
    if (timeUntilExpiry > 60_000) return; // More than 60s — no refresh needed

    try {
      const response: LoginResponse = await refreshTokenClient(tokens.refreshToken);

      tokens = {
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        expiresAt: Date.now() + response.expiresIn * 1000,
        refreshExpiresAt: Date.now() + response.refreshExpiresIn * 1000,
      };
    } catch {
      // Refresh failed — user must re-login
      logout();
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
