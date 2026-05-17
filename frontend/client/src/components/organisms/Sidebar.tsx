import { Link, useMatchRoute } from "@tanstack/react-router";
import { useAuth, type AccessGroup } from "@/lib/auth-context";
import { LayoutDashboard, Users, Building2, Shield, Landmark, UserCheck, BookOpen, Briefcase, Layers } from "lucide-react";

// ---------------------------------------------------------------------------
// Sidebar navigation with permission-based link visibility (D-03, D-19, D-20, D-21)
// ---------------------------------------------------------------------------
// admin-empresa: sees Dashboard, Employees (full), Access Groups, Profile, Fundos
// viewer: sees Employees (read-only), Profile, Fundos (read-only)
// dashboard: sees Dashboard, Employees (read-only), Profile
// ---------------------------------------------------------------------------

interface NavItem {
  label: string;
  href: string;
  icon: React.ReactNode;
  groups: AccessGroup[];
  permission?: string;
}

interface NavGroup {
  label: string;
  permission: string;
  items: NavItem[];
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

// Fundos group: visible to users with funds:read permission (D-23)
const FUNDOS_NAV_GROUP: NavGroup = {
  label: "Fundos",
  permission: "funds:read",
  items: [
    {
      label: "Fundos",
      href: "/fundos",
      icon: <Landmark className="h-5 w-5" />,
      groups: ["admin-empresa", "viewer"],
      permission: "funds:read",
    },
    {
      label: "Cedentes",
      href: "/cedentes",
      icon: <UserCheck className="h-5 w-5" />,
      groups: ["admin-empresa", "viewer"],
      permission: "funds:read",
    },
    {
      label: "Consultorias",
      href: "/consultorias-fundo",
      icon: <Briefcase className="h-5 w-5" />,
      groups: ["admin-empresa", "viewer"],
      permission: "funds:read",
    },
    {
      label: "Custodiantes",
      href: "/custodiantes",
      icon: <BookOpen className="h-5 w-5" />,
      groups: ["admin-empresa", "viewer"],
      permission: "funds:read",
    },
    {
      label: "Tipos de Ativo",
      href: "/tipos-ativos",
      icon: <Layers className="h-5 w-5" />,
      groups: ["admin-empresa", "viewer"],
      permission: "funds:read",
    },
  ],
};

/**
 * Sidebar: fixed left sidebar with navigation links based on user permissions.
 * Hidden when unauthenticated.
 * Fundos group (D-23) visible when user has funds:read permission.
 */
export function Sidebar() {
  const { auth } = useAuth();
  const matchRoute = useMatchRoute();

  if (!auth.isAuthenticated) {
    return null;
  }

  const userGroup = auth.accessGroup;
  // auth.permissions is not on the AuthContextValue type yet; cast to any to avoid breaking change.
  // TODO: add permissions: string[] to AuthContextValue in Phase 51 or next refactor.
  const permissions: string[] = (auth as any).permissions ?? [];

  // Filter nav items based on user's access group
  const visibleItems = userGroup
    ? NAV_ITEMS.filter((item) => item.groups.includes(userGroup))
    : [];

  // Fundos group: visible if user has funds:read permission
  const showFundosGroup = permissions.includes("funds:read");
  const visibleFundosItems = showFundosGroup
    ? FUNDOS_NAV_GROUP.items.filter((item) => item.groups.includes(userGroup!))
    : [];

  const renderLink = (item: NavItem) => {
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
  };

  return (
    <aside className="fixed left-0 top-14 z-40 h-[calc(100vh-3.5rem)] w-64 overflow-y-auto border-r bg-background">
      <nav className="flex h-full flex-col gap-1 p-4">
        {visibleItems.map(renderLink)}

        {showFundosGroup && visibleFundosItems.length > 0 && (
          <>
            <div className="mt-4 mb-1 px-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              {FUNDOS_NAV_GROUP.label}
            </div>
            {visibleFundosItems.map(renderLink)}
          </>
        )}
      </nav>
    </aside>
  );
}