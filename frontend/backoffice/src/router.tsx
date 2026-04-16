import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { AuthLoginPage } from "@/components/pages/AuthLoginPage";
import { AuthCallbackPage } from "@/components/pages/AuthCallbackPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { AdminAccessDeniedPage } from "@/components/pages/AdminAccessDeniedPage";
import { AdminUsersPage } from "@/components/pages/AdminUsersPage";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import { AdminUserEditPage } from "@/components/pages/AdminUserEditPage";
import { CreateAdminPage } from "@/components/pages/CreateAdminPage";
import { PasswordChangePage } from "@/components/pages/PasswordChangePage";
import { AuditLogPage } from "@/components/pages/AuditLogPage";
import { AdminAdministratorsPage } from "@/components/pages/AdminAdministratorsPage";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { useEffect } from "react";

// Root route com notFoundComponent para roteamento type-safe de 404
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// Rota index: / -> redirect para /auth/login
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: IndexRoute,
});

// Auth login (redirect-only): /auth/login
const authLoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/login",
  component: AuthLoginPage,
});

// Auth callback: /auth/callback
const authCallbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/callback",
  component: AuthCallbackPage,
});

// Auth error: /auth/error
const authErrorRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/error",
  component: AuthErrorPage,
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

// Rota admin user edit: /admin/users/$id/edit (MUST be before detail route)
const adminUserEditRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users/$id/edit",
  component: () => {
    const { id } = adminUserEditRoute.useParams();
    return (
      <AdminLayout>
        <AdminUserEditPage userId={id as string} />
      </AdminLayout>
    );
  },
} as any);

// Rota admin create: /admin/create
const adminCreateRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/create",
  component: () => (
    <AdminLayout>
      <CreateAdminPage />
    </AdminLayout>
  ),
});

// Rota password change: /admin/password-change
const adminPasswordChangeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/password-change",
  component: () => (
    <AdminLayout>
      <PasswordChangePage />
    </AdminLayout>
  ),
});

// Rota audit log: /admin/audit-log
const adminAuditLogRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/audit-log",
  component: () => (
    <AdminLayout>
      <AuditLogPage />
    </AdminLayout>
  ),
});

// Rota admin administrators: /admin/administrators
const adminAdministratorsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/administrators",
  component: () => (
    <AdminLayout>
      <AdminAdministratorsPage />
    </AdminLayout>
  ),
});

// Arvore de rotas
const routeTree = rootRoute.addChildren([
  indexRoute,
  authLoginRoute,
  authCallbackRoute,
  authErrorRoute,
  adminAccessDeniedRoute,
  adminUsersRoute,
  adminUserEditRoute,
  adminUserDetailRoute,
  adminCreateRoute,
  adminPasswordChangeRoute,
  adminAuditLogRoute,
  adminAdministratorsRoute,
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
// IndexRoute: redirects to /auth/login
// ---------------------------------------------------------------------------

function IndexRoute() {
  const navigate = useNavigate();

  useEffect(() => {
    navigate({ to: "/auth/login" as any, replace: true });
  }, [navigate]);

  return null;
}
