import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { AdminLoginPage } from "@/components/pages/AdminLoginPage";
import { AdminAccessDeniedPage } from "@/components/pages/AdminAccessDeniedPage";
import { AdminUsersPage } from "@/components/pages/AdminUsersPage";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { useEffect } from "react";

// Root route com notFoundComponent para roteamento type-safe de 404
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// Rota index: / -> redirect para /admin/login
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: IndexRoute,
});

// Rota admin login: /admin/login
const adminLoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/login",
  component: AdminLoginPage,
});

// Rota admin access denied: /admin/access-denied
const adminAccessDeniedRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/access-denied",
  component: AdminAccessDeniedPage,
});

// Rota admin users: /admin/users
const adminUsersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users",
  component: () => (
    <AdminLayout>
      <AdminUsersPage />
    </AdminLayout>
  ),
} as any);

// Rota admin user detail: /admin/users/$id
const adminUserDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users/$id",
  component: () => {
    const { id } = adminUserDetailRoute.useParams();
    return (
      <AdminLayout>
        <AdminUserDetailPage userId={id as string} />
      </AdminLayout>
    );
  },
} as any);

// Arvore de rotas — APENAS rotas admin (sem rotas publicas)
const routeTree = rootRoute.addChildren([
  indexRoute,
  adminLoginRoute,
  adminAccessDeniedRoute,
  adminUsersRoute,
  adminUserDetailRoute,
]);

// Instancia do router
export const router = createRouter({ routeTree });

// Registro obrigatorio para type safety do TypeScript
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

// ---------------------------------------------------------------------------
// IndexRoute: redirects to /admin/login
// ---------------------------------------------------------------------------

function IndexRoute() {
  const navigate = useNavigate();

  useEffect(() => {
    navigate({ to: "/admin/login" as any, replace: true });
  }, [navigate]);

  return null;
}
