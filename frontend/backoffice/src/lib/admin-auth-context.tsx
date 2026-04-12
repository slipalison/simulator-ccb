import { createContext, useContext, useState, useEffect, type ReactNode } from "react";
import { loginAdmin, logoutAdmin, getAdminMe } from "@/lib/admin-api";

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
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
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

  // Session restoration on mount
  useEffect(() => {
    async function tryRestore() {
      try {
        const me = await getAdminMe();
        setAdminName(me.adminName);
        setAdminEmail(me.adminEmail);
        setIsAuthenticated(true);
      } catch {
        // Session invalid — admin needs to login
      } finally {
        setIsLoading(false);
      }
    }

    tryRestore();
  }, []);

  async function login(email: string, password: string): Promise<void> {
    setIsLoading(true);
    try {
      const session = await loginAdmin(email, password);
      setAdminName(session.adminName);
      setAdminEmail(session.adminEmail);
      setIsAuthenticated(true);
    } finally {
      setIsLoading(false);
    }
  }

  async function logout(): Promise<void> {
    try {
      await logoutAdmin();
    } catch {
      // Best effort — clear local state regardless
    }
    setAdminName(null);
    setAdminEmail(null);
    setIsAuthenticated(false);
  }

  async function restoreSession(): Promise<boolean> {
    try {
      const me = await getAdminMe();
      setAdminName(me.adminName);
      setAdminEmail(me.adminEmail);
      setIsAuthenticated(true);
      return true;
    } catch {
      setAdminName(null);
      setAdminEmail(null);
      setIsAuthenticated(false);
      return false;
    }
  }

  return (
    <AdminAuthContext.Provider
      value={{
        admin: { isAuthenticated, isLoading, adminName, adminEmail },
        login,
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
