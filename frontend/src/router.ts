import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
} from "@tanstack/react-router";
import { HomePage } from "@/components/pages/HomePage";
import { NotFoundPage } from "@/components/pages/NotFoundPage";

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

// Árvore de rotas
const routeTree = rootRoute.addChildren([indexRoute]);

// Instância do router
export const router = createRouter({ routeTree });

// Registro obrigatório para type safety do TypeScript
// Sem isso, useNavigate, Link e outros hooks não são type-checked
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
