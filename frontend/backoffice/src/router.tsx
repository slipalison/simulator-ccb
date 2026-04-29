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
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { AdminCompaniesPage } from "@/components/pages/AdminCompaniesPage";
import { AdminEmployeesPage } from "@/components/pages/AdminEmployeesPage";
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

// Rota auth error: /auth/error (exibida pelo SPA quando callback retorna erro)
const authErrorRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/error",
  component: AuthErrorPage,
});

// Rota admin companies: /admin/companies
const adminCompaniesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/companies",
  component: () => (
    <AdminLayout>
      <AdminCompaniesPage />
    </AdminLayout>
  ),
});

// Rota admin employees: /admin/employees
const adminEmployeesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/employees",
  component: () => (
    <AdminLayout>
      <AdminEmployeesPage />
    </AdminLayout>
  ),
});

// Rota admin users (legacy redirect): /admin/users → /admin/companies
const adminUsersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users",
  component: RedirectCompanies,
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

// Arvore de rotas — APENAS rotas admin (sem rotas publicas)
const routeTree = rootRoute.addChildren([
  indexRoute,
  adminLoginRoute,
  adminAccessDeniedRoute,
  authErrorRoute,
  adminUsersRoute,
  adminCompaniesRoute,
  adminEmployeesRoute,
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
// IndexRoute: redirects to /admin/login
// ---------------------------------------------------------------------------

function IndexRoute() {
  const navigate = useNavigate();

  useEffect(() => {
    navigate({ to: "/admin/login" as any, replace: true });
  }, [navigate]);

  return null;
}

function RedirectCompanies() {
  const navigate = useNavigate();

  useEffect(() => {
    navigate({ to: "/admin/companies" as any, replace: true });
  }, [navigate]);

  return null;
}
