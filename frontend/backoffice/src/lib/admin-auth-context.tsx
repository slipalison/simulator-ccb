import { createContext, useContext, useState, useEffect, type ReactNode } from "react";

// ---------------------------------------------------------------------------
// Context definition
// ---------------------------------------------------------------------------

interface AdminAuthValue {
  admin: {
    isAuthenticated: boolean;
    isLoading: boolean;
    adminName: string | null;
    adminEmail: string | null;
  };
  /** Redirects to /auth/login (Vinxi server → Keycloak ACF) */
  login: () => void;
  /** Redirects to /auth/logout (Vinxi server clears cookies → Keycloak OIDC logout) */
  logout: () => void;
}

const AdminAuthContext = createContext<AdminAuthValue | null>(null);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [adminName, setAdminName] = useState<string | null>(null);
  const [adminEmail, setAdminEmail] = useState<string | null>(null);

  // Session restoration on mount via /auth/me
  useEffect(() => {
    async function tryRestore() {
      try {
        const res = await fetch("/auth/me", { credentials: "include" });
        if (res.ok) {
          const data = (await res.json()) as {
            adminName: string;
            email: string;
            isAuthenticated: boolean;
          };
          setAdminName(data.adminName);
          setAdminEmail(data.email);
          setIsAuthenticated(data.isAuthenticated);
        }
      } catch {
        // Session invalid — admin needs to login
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
    <AdminAuthContext.Provider
      value={{
        admin: { isAuthenticated, isLoading, adminName, adminEmail },
        login,
        logout,
      }}
    >
      {children}
    </AdminAuthContext.Provider>
  );
}

// ---------------------------------------------------------------------------
// Hook
// ---------------------------------------------------------------------------

export function useAdminAuth(): AdminAuthValue {
  const context = useContext(AdminAuthContext);
  if (!context) {
    throw new Error("useAdminAuth must be used within an AdminAuthProvider");
  }
  return context;
}
