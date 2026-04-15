import {
  createContext,
  useContext,
  useState,
  useEffect,
  type ReactNode,
} from "react";
import { getAdminMe } from "@/lib/admin-api";

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
  login: () => void;
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

  // Session check on mount via /auth/me (Vinxi server action)
  useEffect(() => {
    async function tryRestore() {
      try {
        const me = await getAdminMe();
        setAdminName(me.adminName);
        setAdminEmail(me.adminEmail);
        setIsAuthenticated(true);
      } catch {
        // No valid session — admin must go through Auth Code Flow
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
