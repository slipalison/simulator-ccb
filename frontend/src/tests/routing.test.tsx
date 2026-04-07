import { describe, it, expect } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { router } from "@/router";

// Criar router com history apontando para rota desconhecida
async function renderWithUnknownRoute() {
  const memoryHistory = createMemoryHistory({ initialEntries: ["/rota-que-nao-existe"] });
  // Criar nova instância do router com history customizado para o teste
  const testRouter = createRouter({
    routeTree: router.options.routeTree,
    history: memoryHistory,
  });
  const result = render(<RouterProvider router={testRouter} />);
  // TanStack Router é assíncrono — aguardar resolução do estado de navegação
  await testRouter.load();
  return result;
}

describe("FRONT-03: TanStack Router — rota 404 type-safe", () => {
  it("navegando para rota desconhecida renderiza NotFoundPage", async () => {
    await renderWithUnknownRoute();
    // NotFoundPage exibe "404" como texto
    await waitFor(() => {
      expect(screen.getByText("404")).toBeInTheDocument();
    });
  });

  it("NotFoundPage contém texto visível de página não encontrada", async () => {
    await renderWithUnknownRoute();
    await waitFor(() => {
      expect(screen.getByText(/página não encontrada/i)).toBeInTheDocument();
    });
  });
});
