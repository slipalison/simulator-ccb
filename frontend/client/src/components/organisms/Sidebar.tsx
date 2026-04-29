import { Link, useMatchRoute } from "@tanstack/react-router";
import { useAuth, type AccessGroup } from "@/lib/auth-context";
import { LayoutDashboard, Users, Building2, Shield } from "lucide-react";

// ---------------------------------------------------------------------------
// Sidebar navigation with permission-based link visibility (D-03, D-19, D-20, D-21)
// ---------------------------------------------------------------------------
// admin-empresa: sees Dashboard, Employees (full), Access Groups, Profile
// viewer: sees Employees (read-only), Profile
// dashboard: sees Dashboard, Employees (read-only), Profile
// ---------------------------------------------------------------------------

interface NavItem {
  label: string;
  href: string;
  icon: React.ReactNode;
  groups: AccessGroup[];
  permission?: string;
}

const NAV_ITEMS: NavItem[] = [
  {
    label: "Dashboard",
    href: "/dashboard",
    icon: <LayoutDashboard className="h-5 w-5" />,
    groups: ["admin-empresa", "dashboard"],
  },
  {
    label: "Funcionários",
    href: "/employees",
    icon: <Users className="h-5 w-5" />,
    groups: ["admin-empresa", "viewer", "dashboard"],
  },
  {
    label: "Grupos de Acesso",
    href: "/access-groups",
    icon: <Shield className="h-5 w-5" />,
    groups: ["admin-empresa"],
    permission: "access-groups:manage",
  },
  {
    label: "Perfil Empresa",
    href: "/profile",
    icon: <Building2 className="h-5 w-5" />,
    groups: ["admin-empresa", "viewer", "dashboard"],
  },
];

/**
 * Sidebar: fixed left sidebar with navigation links based on user permissions.
 * Hidden when unauthenticated.
 */
export function Sidebar() {
  const { auth } = useAuth();
  const matchRoute = useMatchRoute();

  if (!auth.isAuthenticated) {
    return null;
  }

  const userGroup = auth.accessGroup;

  // Filter nav items based on user's access group
  const visibleItems = userGroup
    ? NAV_ITEMS.filter((item) => item.groups.includes(userGroup))
    : [];

  return (
    <aside className="fixed left-0 top-14 z-40 h-[calc(100vh-3.5rem)] w-64 border-r bg-background">
      <nav className="flex h-full flex-col gap-1 p-4">
        {visibleItems.map((item) => {
          const isActive = matchRoute({ to: item.href as any });

          return (
            <Link
              key={item.href}
              to={item.href as any}
              className={`flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                isActive
                  ? "bg-primary/10 text-primary"
                  : "text-muted-foreground hover:bg-muted hover:text-foreground"
              }`}
            >
              {item.icon}
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}