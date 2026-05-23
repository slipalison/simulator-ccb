import { type ReactNode, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { toast } from "sonner";
import { LogOut, Shield, Loader2, ChevronDown } from "lucide-react";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

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
// AdminSidebar — includes Fundos group (D-31, Phase 52)
// Authenticated-only: shown when admin.isAuthenticated (checked by AdminLayout)
// ---------------------------------------------------------------------------

function AdminSidebar() {
  return (
    <aside className="w-56 border-r bg-card p-4 overflow-y-auto" data-testid="admin-sidebar">
      <nav className="space-y-1">
        <a
          href="/admin/companies"
          className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
          data-testid="sidebar-companies-link"
        >
          Empresas
        </a>
        <a
          href="/admin/users"
          className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
          data-testid="sidebar-users-link"
        >
          Usuários
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

        {/* Fundos group — Phase 52 (D-31) */}
        <div className="pt-2" data-testid="sidebar-fundos-group">
          <div className="flex items-center gap-1 px-3 py-1 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            <ChevronDown className="h-3 w-3" aria-hidden="true" />
            <span>{L.sidebarFundosGroup}</span>
          </div>
          <div className="mt-1 space-y-1 pl-2">
            <a
              href="/admin/fundos"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-fundos-link"
            >
              {L.sidebarFundos}
            </a>
            <a
              href="/admin/cedentes"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-cedentes-link"
            >
              {L.sidebarCedentes}
            </a>
            <a
              href="/admin/consultorias-fundo"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-consultorias-fundo-link"
            >
              {L.sidebarConsultoriasFundo}
            </a>
            <a
              href="/admin/custodiantes"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-custodiantes-link"
            >
              {L.sidebarCustodiantes}
            </a>
            <a
              href="/admin/fundo-cedentes"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-fundo-cedentes-link"
            >
              {L.sidebarFundoCedentes}
            </a>
            <a
              href="/admin/fundo-tipos-ativos"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-fundo-tipos-ativos-link"
            >
              {L.sidebarFundoTiposAtivos}
            </a>
            <a
              href="/admin/cedente-tipos-ativos"
              className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
              data-testid="sidebar-cedente-tipos-ativos-link"
            >
              {L.sidebarCedenteTiposAtivos}
            </a>
          </div>
        </div>
      </nav>
    </aside>
  );
}

// ---------------------------------------------------------------------------
// AdminLayout
// ---------------------------------------------------------------------------

export function AdminLayout({ children }: { children: ReactNode }) {
  const { admin, logout } = useAdminAuth();

  // Redirect only after loading completes and auth is confirmed absent.
  // Guard is gated on !isLoading to prevent firing during the initial tryRestore
  // window (which includes the single 401 retry — see admin-auth-context.tsx + D-12).
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

  // Render a loading shell while session restoration is in progress.
  // Returning null here caused a one-frame flash before the redirect effect fired —
  // the shell prevents visible content shift and the redirect effect is unchanged.
  if (admin.isLoading) {
    return (
      <div
        className="flex min-h-screen items-center justify-center bg-background"
        data-testid="admin-loading-shell"
        aria-label="Carregando"
        aria-busy="true"
      >
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-hidden="true" />
      </div>
    );
  }

  if (!admin.isAuthenticated) {
    // Redirect will fire via the effect above; render nothing while the navigation
    // is in flight so unauthenticated content is never shown.
    return null;
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
