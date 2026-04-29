import { type ReactNode, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { toast } from "sonner";
import { LogOut, Shield } from "lucide-react";

// ---------------------------------------------------------------------------
// AdminHeader
// ---------------------------------------------------------------------------

interface AdminHeaderProps {
  adminName: string;
  onLogout: () => void;
}

export function AdminHeader({ adminName, onLogout }: AdminHeaderProps) {
  return (
    <header className="border-b px-6 py-4 flex items-center justify-between bg-card">
      <div className="flex items-center gap-3">
        <Shield className="h-6 w-6 text-primary" />
        <h1 className="text-lg font-semibold">Backoffice Admin</h1>
      </div>
      <div className="flex items-center gap-4">
        <span className="text-sm text-muted-foreground" data-testid="admin-greeting">
          Ola, {adminName}
        </span>
        <Button
          variant="outline"
          size="sm"
          onClick={onLogout}
          data-testid="admin-logout-button"
        >
          <LogOut className="h-4 w-4 mr-1" />
          Logout
        </Button>
      </div>
    </header>
  );
}

// ---------------------------------------------------------------------------
// AdminSidebar
// ---------------------------------------------------------------------------

function AdminSidebar() {
  return (
    <aside className="w-56 border-r bg-card p-4" data-testid="admin-sidebar">
      <nav className="space-y-1">
        <a
          href="/admin/companies"
          className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
          data-testid="sidebar-companies-link"
        >
          Empresas
        </a>
        <a
          href="/admin/employees"
          className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
          data-testid="sidebar-employees-link"
        >
          Funcionários
        </a>
        <a
          href="/admin/administrators"
          className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
          data-testid="sidebar-administrators-link"
        >
          Administradores
        </a>
        <a
          href="/admin/audit-log"
          className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
          data-testid="sidebar-audit-log-link"
        >
          Audit Log
        </a>
      </nav>
    </aside>
  );
}

// ---------------------------------------------------------------------------
// AdminLayout
// ---------------------------------------------------------------------------

export function AdminLayout({ children }: { children: ReactNode }) {
  const { admin, logout } = useAdminAuth();

  useEffect(() => {
    if (!admin.isLoading && !admin.isAuthenticated) {
      window.location.href = "/admin/login";
    }
  }, [admin.isLoading, admin.isAuthenticated]);

  async function handleLogout() {
    await logout();
    toast.success("Logout realizado", { description: "Ate logo!" });
    window.location.href = "/admin/login";
  }

  if (admin.isLoading || !admin.isAuthenticated) {
    return null; // Prevents flashing unauthenticated content
  }

  return (
    <div className="flex flex-col min-h-screen bg-background" data-testid="admin-layout">
      <AdminHeader
        adminName={admin.adminName || "Admin"}
        onLogout={handleLogout}
      />
      <div className="flex flex-1">
        <AdminSidebar />
        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  );
}
