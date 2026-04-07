import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
} from "@tanstack/react-router";
import { HomePage } from "@/components/pages/HomePage";
import { NotFoundPage } from "@/components/pages/NotFoundPage";
import { RegistrationPage } from "@/components/pages/RegistrationPage";
import { LoginPage } from "@/components/pages/LoginPage";
import { ProfilePage } from "@/components/pages/ProfilePage";

// Root route com notFoundComponent para roteamento type-safe de 404
// NOTA: NotFoundRoute (classe) está depreciada — usar notFoundComponent no rootRoute
const rootRoute = createRootRoute({
  component: () => <Outlet />,
  notFoundComponent: NotFoundPage,
});

// Rota index: /
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: HomePage,
});

// Rota de registro: /registration
const registrationRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/registration",
  component: RegistrationPage,
});

// Rota de login: /login
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: LoginPage,
});

// Rota de perfil: /profile (protegida — Phase 10)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/profile",
  component: ProfilePage,
} as any);

// Árvore de rotas
const routeTree = rootRoute.addChildren([indexRoute, registrationRoute, loginRoute, profileRoute]);

// Instância do router
export const router = createRouter({ routeTree });

// Registro obrigatório para type safety do TypeScript
// Sem isso, useNavigate, Link e outros hooks não são type-checked
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
