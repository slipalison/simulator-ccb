import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { RegistrationForm } from "@/components/molecules/RegistrationForm";
import { LoginPage } from "@/components/pages/LoginPage";
import { ProfilePage } from "@/components/pages/ProfilePage";
import { ForgotPasswordPage } from "@/components/pages/ForgotPasswordPage";
import { ResetPasswordPage } from "@/components/pages/ResetPasswordPage";
import { AdminLoginPage } from "@/components/pages/AdminLoginPage";
import { AdminAccessDeniedPage } from "@/components/pages/AdminAccessDeniedPage";
import { AdminUsersPage } from "@/components/pages/AdminUsersPage";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { useAuth } from "@/lib/auth-context";
import { useEffect } from "react";
import { z } from "zod";

// Root route com notFoundComponent para roteamento type-safe de 404
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// Rota index: / -> LoginPage (se nao logado) ou redirect para /profile (se logado)
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: RootRoute,
});

// Rota de registro: /register (formulario unico PF/PJ)
const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/register",
  component: RegistrationForm,
});

// Rota de login: /login
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: LoginPage,
});

// Rota de perfil: /profile (protegida)
const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/profile",
  component: ProfilePage,
} as any);

// Rota de forgot password: /forgot-password
const forgotPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/forgot-password",
  component: ForgotPasswordPage,
});

// Rota de reset password: /reset-password?token=xxx
const resetPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/reset-password",
  component: ResetPasswordPage,
  validateSearch: z.object({ token: z.string().optional() }),
});

// ---------------------------------------------------------------------------
// Admin Routes — /admin/*
// These use a separate AdminAuthProvider (no session conflicts with user auth)
// ---------------------------------------------------------------------------

const adminLoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/login",
  component: AdminLoginPage,
});

const adminAccessDeniedRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/access-denied",
  component: AdminAccessDeniedPage,
});

const adminUsersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/users",
  component: () => (
    <AdminLayout>
      <AdminUsersPage />
    </AdminLayout>
  ),
} as any);

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

// Arvore de rotas
const routeTree = rootRoute.addChildren([
  indexRoute,
  registerRoute,
  loginRoute,
  profileRoute,
  forgotPasswordRoute,
  resetPasswordRoute,
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
// RootRoute: shows LoginPage for unauthenticated users
// If authenticated, useEffect will redirect to /profile
// ---------------------------------------------------------------------------

function RootRoute() {
  const { auth } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (auth.isAuthenticated) {
      navigate({ to: "/profile" as any, replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  return <LoginPage />;
}
