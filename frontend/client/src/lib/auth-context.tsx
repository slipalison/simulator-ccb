import { createContext, useContext, useState, useEffect, type ReactNode } from "react";

// ---------------------------------------------------------------------------
// Context definition
// ---------------------------------------------------------------------------

interface AuthContextValue {
  auth: {
    isAuthenticated: boolean;
    isLoading: boolean;
    userName: string | null;
    email: string | null;
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

  // Session restoration on mount via /auth/me
  useEffect(() => {
    async function tryRestore() {
      try {
        const res = await fetch("/auth/me", { credentials: "include" });
        if (res.ok) {
          const data = (await res.json()) as {
            userName: string;
            email: string;
            isAuthenticated: boolean;
          };
          setUserName(data.userName);
          setEmail(data.email);
          setIsAuthenticated(data.isAuthenticated);
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
        auth: { isAuthenticated, isLoading, userName, email },
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
