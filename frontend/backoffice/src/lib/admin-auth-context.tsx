import { createContext, useContext, useState, useEffect, type ReactNode } from "react";
import { logoutAdmin, getAdminMe, AdminApiError } from "@/lib/admin-api";

// ---------------------------------------------------------------------------
// Context definition
// ---------------------------------------------------------------------------

interface AdminAuthValue {
  admin: {
    isAuthenticated: boolean;
    isLoading: boolean;
    adminName: string | null;
    adminEmail: string | null;
    adminId: string | null;
  };
  logout: () => void;
  restoreSession: () => Promise<boolean>;
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
  const [adminId, setAdminId] = useState<string | null>(null);

  // Session restoration on mount
  useEffect(() => {
    async function tryRestore() {
      try {
        const me = await getAdminMe();
        setAdminName(me.adminName);
        setAdminEmail(me.adminEmail);
        setAdminId(me.adminId);
        setIsAuthenticated(true);
      } catch (err) {
        const is401 = err instanceof AdminApiError && err.status === 401;
        if (is401) {
          // D-12 + post-redirect cookie-commit race: if the first /auth/me races the cookie
          // write after the Keycloak callback redirect, retry exactly once after 200 ms.
          await new Promise<void>((r) => setTimeout(r, 200));
          try {
            const me = await getAdminMe();
            setAdminName(me.adminName);
            setAdminEmail(me.adminEmail);
            setAdminId(me.adminId);
            setIsAuthenticated(true);
          } catch {
            // Second 401 — cookie genuinely absent. Finalize as not authenticated.
          }
        }
        // 5xx or network error: fail fast, do not retry — surface as not authenticated.
      } finally {
        setIsLoading(false);
      }
    }

    tryRestore();
  }, []);

  function logout(): void {
    setAdminName(null);
    setAdminEmail(null);
    setAdminId(null);
    setIsAuthenticated(false);
    logoutAdmin();
  }

  async function restoreSession(): Promise<boolean> {
    try {
      const me = await getAdminMe();
      setAdminName(me.adminName);
      setAdminEmail(me.adminEmail);
      setAdminId(me.adminId);
      setIsAuthenticated(true);
      return true;
    } catch {
      setAdminName(null);
      setAdminEmail(null);
      setAdminId(null);
      setIsAuthenticated(false);
      return false;
    }
  }

  return (
    <AdminAuthContext.Provider
      value={{
        admin: { isAuthenticated, isLoading, adminName, adminEmail, adminId },
        logout,
        restoreSession,
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
