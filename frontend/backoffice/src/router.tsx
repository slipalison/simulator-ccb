import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { AdminAccessDeniedPage } from "@/components/pages/AdminAccessDeniedPage";
import { AdminUsersPage } from "@/components/pages/AdminUsersPage";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import { AdminUserEditPage } from "@/components/pages/AdminUserEditPage";
import { CreateAdminPage } from "@/components/pages/CreateAdminPage";
import { PasswordChangePage } from "@/components/pages/PasswordChangePage";
import { AuditLogPage } from "@/components/pages/AuditLogPage";
import { AuthLoginPage } from "@/components/pages/AuthLoginPage";
import { AuthCallbackPage } from "@/components/pages/AuthCallbackPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { useEffect } from "react";

// Root route
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// ---------------------------------------------------------------------------
// Auth routes (public — no guard)
// ---------------------------------------------------------------------------

const authLoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/login",
  component: AuthLoginPage,
});

const authCallbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/callback",
  component: AuthCallbackPage,
});

const authErrorRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/error",
  component: AuthErrorPage,
});

// ---------------------------------------------------------------------------
// Protected admin routes
// ---------------------------------------------------------------------------

// Rota index: / -> redirect para /admin/users
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: IndexRoute,
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
    <ProtectedRoute>
      <AdminLayout>
        <AdminUsersPage />
      </AdminLayout>
    </ProtectedRoute>
  ),
} as any);

// Rota admin user detail: /admin/users/$id
const adminUserDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users/$id",
  component: () => {
    const { id } = adminUserDetailRoute.useParams();
    return (
      <ProtectedRoute>
        <AdminLayout>
          <AdminUserDetailPage userId={id as string} />
        </AdminLayout>
      </ProtectedRoute>
    );
  },
} as any);

// Rota admin user edit: /admin/users/$id/edit
const adminUserEditRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users/$id/edit",
  component: () => {
    const { id } = adminUserEditRoute.useParams();
    return (
      <ProtectedRoute>
        <AdminLayout>
          <AdminUserEditPage userId={id as string} />
        </AdminLayout>
      </ProtectedRoute>
    );
  },
} as any);

// Rota admin create: /admin/create
const adminCreateRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/create",
  component: () => (
    <ProtectedRoute>
      <AdminLayout>
        <CreateAdminPage />
      </AdminLayout>
    </ProtectedRoute>
  ),
});

// Rota password change: /admin/password-change
const adminPasswordChangeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/password-change",
  component: () => (
    <ProtectedRoute>
      <AdminLayout>
        <PasswordChangePage />
      </AdminLayout>
    </ProtectedRoute>
  ),
});

// Rota audit log: /admin/audit-log
const adminAuditLogRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/audit-log",
  component: () => (
    <ProtectedRoute>
      <AdminLayout>
        <AuditLogPage />
      </AdminLayout>
    </ProtectedRoute>
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
// IndexRoute: redirect to /admin/users (ProtectedRoute handles auth check)
// ---------------------------------------------------------------------------

function IndexRoute() {
  const navigate = useNavigate();

  useEffect(() => {
    navigate({ to: "/admin/users" as any, replace: true });
  }, [navigate]);

  return null;
}

// ---------------------------------------------------------------------------
// ProtectedRoute: redirect to /auth/login if not authenticated
// ---------------------------------------------------------------------------

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { admin, login } = useAdminAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!admin.isLoading && !admin.isAuthenticated) {
      login();
    }
  }, [admin.isLoading, admin.isAuthenticated, login, navigate]);

  if (admin.isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p className="text-muted-foreground">Carregando...</p>
      </div>
    );
  }

  if (!admin.isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
