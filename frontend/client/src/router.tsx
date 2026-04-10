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

// Arvore de rotas — APENAS rotas publicas (sem /admin/*)
const routeTree = rootRoute.addChildren([
  indexRoute,
  registerRoute,
  loginRoute,
  profileRoute,
  forgotPasswordRoute,
  resetPasswordRoute,
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
