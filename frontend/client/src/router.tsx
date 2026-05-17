import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { RegisterPage } from "@/components/pages/RegisterPage";
import { AuthLoginPage } from "@/components/pages/AuthLoginPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { ProfilePage } from "@/components/pages/ProfilePage";
import { DashboardPage } from "@/components/pages/DashboardPage";
import { EmployeesPage } from "@/components/pages/EmployeesPage";
import { AccessGroupsPage } from "@/components/pages/AccessGroupsPage";
import { ForgotPasswordPage } from "@/components/pages/ForgotPasswordPage";
import { ResetPasswordPage } from "@/components/pages/ResetPasswordPage";
import { AppLayout } from "@/components/templates/AppLayout";
import { useAuth, getDefaultRouteForGroup } from "@/lib/auth-context";
import { z } from "zod";

// Fundos module pages (Phase 51)
import { TiposAtivoListPage } from "@/components/pages/TiposAtivoListPage";
import { ConsultoriasFundoListPage } from "@/components/pages/ConsultoriasFundoListPage";
import { CustodiantesListPage } from "@/components/pages/CustodiantesListPage";
import { CedentesListPage } from "@/components/pages/CedentesListPage";
import { CedenteDetailPage } from "@/components/pages/CedenteDetailPage";
import { FundosListPage } from "@/components/pages/FundosListPage";
import { FundoDetailPage } from "@/components/pages/FundoDetailPage";
import { paginatedSearchSchema, fundoListSearchSchema } from "@/lib/fundos-schemas";

// Root route: notFoundComponent for type-safe 404 routing
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// Auth guard layout — wraps authenticated routes with AppLayout + Sidebar
const authenticatedRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "authenticated",
  component: AppLayout,
});

// Dashboard: /dashboard (default for admin-empresa and dashboard groups)
const dashboardRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/dashboard",
  component: DashboardPage,
});

// Employees: /employees (admin-empresa sees all actions, viewer sees read-only)
const employeesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/employees",
  component: EmployeesPage,
});

// Access Groups: /access-groups (admin-empresa manages groups)
const accessGroupsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/access-groups",
  component: AccessGroupsPage,
});

// Company Profile: /profile (visible to all authenticated groups)
const profileRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/profile",
  component: ProfilePage,
} as any);

// Registration wizard: /register (PJ-only, no sidebar — full page)
const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/register",
  component: RegisterPage,
});

// Auth login (redirect-only): /auth/login
const authLoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/login",
  component: AuthLoginPage,
});

// Auth error: /auth/error
const authErrorRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/auth/error",
  component: AuthErrorPage,
});

// Forgot password: /forgot-password
const forgotPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/forgot-password",
  component: ForgotPasswordPage,
});

// Reset password: /reset-password?token=xxx
const resetPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/reset-password",
  component: ResetPasswordPage,
  validateSearch: z.object({ token: z.string().optional() }),
});

// Index route: / → redirect to /dashboard if authenticated, or show login page
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: RootRoute,
});

// ---------------------------------------------------------------------------
// Fundos module routes (Phase 51)
// ---------------------------------------------------------------------------

// TipoAtivo: /tipos-ativos
const tiposAtivoRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/tipos-ativos",
  component: TiposAtivoListPage,
  validateSearch: paginatedSearchSchema,
});

// ConsultoriaFundo: /consultorias-fundo
const consultoriasFundoRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/consultorias-fundo",
  component: ConsultoriasFundoListPage,
  validateSearch: paginatedSearchSchema,
});

// Custodiante: /custodiantes
const custodiantesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/custodiantes",
  component: CustodiantesListPage,
  validateSearch: paginatedSearchSchema,
});

// Cedente list: /cedentes
const cedentesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/cedentes",
  component: CedentesListPage,
  validateSearch: paginatedSearchSchema,
});

// Cedente detail: /cedentes/$cedenteId
const cedenteDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/cedentes/$cedenteId",
  component: CedenteDetailPage,
});

// Fundos list: /fundos
const fundosRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fundos",
  component: FundosListPage,
  validateSearch: fundoListSearchSchema,
});

// Fundo detail: /fundos/$fundoId
const fundoDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fundos/$fundoId",
  component: FundoDetailPage,
});

// Route tree
const routeTree = rootRoute.addChildren([
  authenticatedRoute.addChildren([
    dashboardRoute,
    employeesRoute,
    accessGroupsRoute,
    profileRoute,
    // Fundos module routes (Phase 51)
    tiposAtivoRoute,
    consultoriasFundoRoute,
    custodiantesRoute,
    cedentesRoute,
    cedenteDetailRoute,
    fundosRoute,
    fundoDetailRoute,
  ]),
  registerRoute,
  authLoginRoute,
  authErrorRoute,
  forgotPasswordRoute,
  resetPasswordRoute,
  indexRoute,
]);

// Router instance
export const router = createRouter({ routeTree });

// Mandatory TypeScript registration for type safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

// ---------------------------------------------------------------------------
// RootRoute: shows AuthLoginPage for unauthenticated users
// If authenticated, redirect based on access group (D-22)
// ---------------------------------------------------------------------------

function RootRoute() {
  const { auth } = useAuth();
  const navigate = useNavigate();

  if (auth.isAuthenticated) {
    // D-22: redirect based on group — admin-empresa/viewer → /employees, dashboard → /dashboard
    const defaultRoute = getDefaultRouteForGroup(auth.accessGroup);
    navigate({ to: defaultRoute as any, replace: true });
    return null;
  }

  return <AuthLoginPage />;
}