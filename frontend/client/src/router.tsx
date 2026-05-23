import {
  createRootRoute,
  createRoute,
  createRouter,
  getRouteApi,
  Outlet,
  useNavigate,
  lazyRouteComponent,
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
// Fundos module routes (Phase 51) — LAZY per D-32 (T-3 retroactive code-split)
// ---------------------------------------------------------------------------

// TipoAtivo: /tipos-ativos
const tiposAtivoRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/tipos-ativos",
  component: lazyRouteComponent(
    () => import("@/components/pages/TiposAtivoListPage").then((m) => ({ default: m.TiposAtivoListPage }))
  ),
  validateSearch: paginatedSearchSchema,
});

// ConsultoriaFundo: /consultorias-fundo
const consultoriasFundoRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/consultorias-fundo",
  component: lazyRouteComponent(
    () => import("@/components/pages/ConsultoriasFundoListPage").then((m) => ({ default: m.ConsultoriasFundoListPage }))
  ),
  validateSearch: paginatedSearchSchema,
});

// Custodiante: /custodiantes
const custodiantesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/custodiantes",
  component: lazyRouteComponent(
    () => import("@/components/pages/CustodiantesListPage").then((m) => ({ default: m.CustodiantesListPage }))
  ),
  validateSearch: paginatedSearchSchema,
});

// Cedente list: /cedentes
const cedentesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/cedentes",
  component: lazyRouteComponent(
    () => import("@/components/pages/CedentesListPage").then((m) => ({ default: m.CedentesListPage }))
  ),
  validateSearch: paginatedSearchSchema,
});

// Cedente detail: /cedentes/$cedenteId
const cedenteDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/cedentes/$cedenteId",
  component: lazyRouteComponent(
    () => import("@/components/pages/CedenteDetailPage").then((m) => ({ default: m.CedenteDetailPage }))
  ),
});

// Fundos list: /fundos
const fundosRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fundos",
  component: lazyRouteComponent(
    () => import("@/components/pages/FundosListPage").then((m) => ({ default: m.FundosListPage }))
  ),
  validateSearch: fundoListSearchSchema,
});

// Fundo detail: /fundos/$fundoId
const fundoDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fundos/$fundoId",
  component: lazyRouteComponent(
    () => import("@/components/pages/FundoDetailPage").then((m) => ({ default: m.FundoDetailPage }))
  ),
});

// Route tree
const routeTree = rootRoute.addChildren([
  authenticatedRoute.addChildren([
    dashboardRoute,
    employeesRoute,
    accessGroupsRoute,
    profileRoute,
    // Fundos module routes (Phase 51) — lazy per D-32
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

// ---------------------------------------------------------------------------
// Route API instances — canonical v1 pattern for type-safe hooks in page
// components that live outside the route definition (Path A fix for iter 5).
// Using getRouteApi with path string resolves correctly regardless of the
// pathless parent layout route id ("authenticated").
// ---------------------------------------------------------------------------
// Route IDs for children of the pathless "authenticated" layout route are
// prefixed with /authenticated/<path> — NOT bare /<path>.
// Using the full internal route ID is required for getRouteApi to resolve correctly.
export const tiposAtivoRouteApi = getRouteApi("/authenticated/tipos-ativos");
export const consultoriasFundoRouteApi = getRouteApi("/authenticated/consultorias-fundo");
export const custodiantesRouteApi = getRouteApi("/authenticated/custodiantes");
export const cedentesRouteApi = getRouteApi("/authenticated/cedentes");
export const cedenteDetailRouteApi = getRouteApi("/authenticated/cedentes/$cedenteId");
export const fundosRouteApi = getRouteApi("/authenticated/fundos");
export const fundoDetailRouteApi = getRouteApi("/authenticated/fundos/$fundoId");

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
