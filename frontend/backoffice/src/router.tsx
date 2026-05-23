import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
  lazyRouteComponent,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { AdminLoginPage } from "@/components/pages/AdminLoginPage";
import { AdminAccessDeniedPage } from "@/components/pages/AdminAccessDeniedPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { AdminCompaniesPage } from "@/components/pages/AdminCompaniesPage";
import { AdminEmployeesPage } from "@/components/pages/AdminEmployeesPage";
import { AdminUsersPage } from "@/components/pages/AdminUsersPage";
import { CreateAdminPage } from "@/components/pages/CreateAdminPage";
import { PasswordChangePage } from "@/components/pages/PasswordChangePage";
import { AuditLogPage } from "@/components/pages/AuditLogPage";
import { AdminAdministratorsPage } from "@/components/pages/AdminAdministratorsPage";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { adminListSearchSchema } from "@/lib/admin-fundos-schemas";
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

// Rota admin users: /admin/users — user management list
const adminUsersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users",
  component: () => (
    <AdminLayout>
      <AdminUsersPage />
    </AdminLayout>
  ),
});

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

// ---------------------------------------------------------------------------
// Fundos module routes (Phase 52, D-28..D-32) — ALL lazy per D-32
// ---------------------------------------------------------------------------

// /admin/fundos — list page
const adminFundosRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/fundos",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminFundosListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminFundosListPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/fundos/$fundoId — detail page (lazy)
const adminFundoDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/fundos/$fundoId",
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminFundoDetailPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminFundoDetailPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/cedentes — list page (lazy)
const adminCedentesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/cedentes",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminCedentesListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminCedentesListPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/cedentes/$cedenteId — detail page (lazy)
const adminCedenteDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/cedentes/$cedenteId",
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminCedenteDetailPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminCedenteDetailPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/consultorias-fundo — list page (lazy)
const adminConsultoriasFundoRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/consultorias-fundo",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminConsultoriasFundoListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminConsultoriasFundoListPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/consultorias-fundo/$consultoriaId — detail page (lazy)
const adminConsultoriaFundoDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/consultorias-fundo/$consultoriaId",
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminConsultoriaFundoDetailPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminConsultoriaFundoDetailPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/custodiantes — list page (lazy)
const adminCustodiantesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/custodiantes",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminCustodiantesListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminCustodiantesListPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/custodiantes/$custodianteId — detail page (lazy)
const adminCustodianteDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/custodiantes/$custodianteId",
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminCustodianteDetailPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminCustodianteDetailPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/fundo-cedentes — N-N association list (lazy)
const adminFundoCedentesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/fundo-cedentes",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminFundoCedentesListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminFundoCedentesListPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/fundo-tipos-ativos — N-N association list (lazy)
const adminFundoTiposAtivosRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/fundo-tipos-ativos",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminFundoTiposAtivosListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminFundoTiposAtivosListPage />
          </AdminLayout>
        ),
      }))
  ),
});

// /admin/cedente-tipos-ativos — N-N association list (lazy)
const adminCedenteTiposAtivosRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/cedente-tipos-ativos",
  validateSearch: adminListSearchSchema,
  component: lazyRouteComponent(
    () =>
      import("@/components/pages/AdminCedenteTiposAtivosListPage").then((m) => ({
        default: () => (
          <AdminLayout>
            <m.AdminCedenteTiposAtivosListPage />
          </AdminLayout>
        ),
      }))
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
  // Fundos module — Phase 52 (D-28..D-32)
  adminFundosRoute,
  adminFundoDetailRoute,
  adminCedentesRoute,
  adminCedenteDetailRoute,
  adminConsultoriasFundoRoute,
  adminConsultoriaFundoDetailRoute,
  adminCustodiantesRoute,
  adminCustodianteDetailRoute,
  adminFundoCedentesRoute,
  adminFundoTiposAtivosRoute,
  adminCedenteTiposAtivosRoute,
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
