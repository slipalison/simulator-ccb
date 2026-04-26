import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { RegistrationForm } from "@/components/molecules/RegistrationForm";
import { AuthLoginPage } from "@/components/pages/AuthLoginPage";
import { AuthCallbackPage } from "@/components/pages/AuthCallbackPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { ProfilePage } from "@/components/pages/ProfilePage";
import { DashboardPage } from "@/components/pages/DashboardPage";
import { EmployeesPage } from "@/components/pages/EmployeesPage";
import { ForgotPasswordPage } from "@/components/pages/ForgotPasswordPage";
import { ResetPasswordPage } from "@/components/pages/ResetPasswordPage";
import { AppLayout } from "@/components/templates/AppLayout";
import { useAuth } from "@/lib/auth-context";
import { z } from "zod";

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
  component: RegistrationForm,
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

// Route tree
const routeTree = rootRoute.addChildren([
  indexRoute,
  registerRoute,
  authLoginRoute,
  authCallbackRoute,
  authErrorRoute,
  forgotPasswordRoute,
  resetPasswordRoute,
  authenticatedRoute.addChildren([
    dashboardRoute,
    employeesRoute,
    profileRoute,
  ]),
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
// If authenticated, redirect to /dashboard
// ---------------------------------------------------------------------------

function RootRoute() {
  const { auth } = useAuth();
  const navigate = useNavigate();

  if (auth.isAuthenticated) {
    // Navigate to dashboard instead of profile (D-02: / is dashboard for auth users)
    navigate({ to: "/dashboard" as any, replace: true });
    return null;
  }

  return <AuthLoginPage />;
}