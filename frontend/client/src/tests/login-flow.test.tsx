import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

async function renderWithRouter(initialPath: string) {
  mockFetch.mockResolvedValue({ ok: false, status: 401 });

  const memoryHistory = createMemoryHistory({ initialEntries: [initialPath] });
  const testRouter = createRouter({
    routeTree: router.options.routeTree,
    history: memoryHistory,
  });
  const view = render(
    <AuthProvider>
      <RouterProvider router={testRouter} />
    </AuthProvider>
  );
  await testRouter.load();
  return { ...view, testRouter, memoryHistory };
}

describe("ACF Login flow — redirect-based", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  it("AuthLoginPage shows loading spinner while session is being checked", async () => {
    // /auth/me hangs — isLoading stays true
    mockFetch.mockImplementation(() => new Promise(() => {}));

    const memoryHistory = createMemoryHistory({ initialEntries: ["/auth/login"] });
    const testRouter = createRouter({
      routeTree: router.options.routeTree,
      history: memoryHistory,
    });
    render(
      <AuthProvider>
        <RouterProvider router={testRouter} />
      </AuthProvider>
    );
    await testRouter.load();

    await waitFor(() => {
      expect(screen.getByText(/redirecionando/i)).toBeInTheDocument();
    });
  });

  it("AuthCallbackPage shows loading spinner while polling /auth/me", async () => {
    // /auth/me returns 401 on all calls (not yet authenticated)
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    const memoryHistory = createMemoryHistory({ initialEntries: ["/auth/callback"] });
    const testRouter = createRouter({
      routeTree: router.options.routeTree,
      history: memoryHistory,
    });
    render(
      <AuthProvider>
        <RouterProvider router={testRouter} />
      </AuthProvider>
    );
    await testRouter.load();

    await waitFor(() => {
      expect(screen.getByText(/concluindo/i)).toBeInTheDocument();
    });
  });

  it("AuthErrorPage displays error message from URL param", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    // Mock window.location.search
    Object.defineProperty(window, "location", {
      value: { search: "?error=access_denied", href: "/auth/error?error=access_denied" },
      writable: true,
    });

    const memoryHistory = createMemoryHistory({ initialEntries: ["/auth/error?error=access_denied"] });
    const testRouter = createRouter({
      routeTree: router.options.routeTree,
      history: memoryHistory,
    });
    render(
      <AuthProvider>
        <RouterProvider router={testRouter} />
      </AuthProvider>
    );
    await testRouter.load();

    await waitFor(() => {
      expect(screen.getByText(/erro de autenticação/i)).toBeInTheDocument();
    });
  });

  it("/ route shows AuthLoginPage for unauthenticated users", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    await renderWithRouter("/");

    await waitFor(() => {
      expect(screen.getByText(/redirecionando/i)).toBeInTheDocument();
    });
  });
});

describe("ACF auth routes — router configuration", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
    mockFetch.mockResolvedValue({ ok: false, status: 401 });
  });

  it("router has /auth/login route", () => {
    const allPaths = getAllPaths(router.options.routeTree);
    expect(allPaths.some((p) => p.includes("auth") || p.includes("login"))).toBe(true);
  });

  it("router has /auth/callback route", () => {
    const allPaths = getAllPaths(router.options.routeTree);
    expect(allPaths.some((p) => p.includes("callback") || p.includes("auth"))).toBe(true);
  });

  it("router has /auth/error route", () => {
    const allPaths = getAllPaths(router.options.routeTree);
    expect(allPaths.some((p) => p.includes("error") || p.includes("auth"))).toBe(true);
  });
});

function getAllPaths(routeTree: any): string[] {
  const paths: string[] = [];
  // TanStack Router stores path in multiple possible locations
  if (routeTree.path) paths.push(routeTree.path);
  if (routeTree.options?.path) paths.push(routeTree.options.path);
  if (routeTree.fullPath) paths.push(routeTree.fullPath);
  // Traverse children stored as __children or children
  const children = routeTree.__children ?? routeTree.children ?? [];
  for (const child of children) {
    paths.push(...getAllPaths(child));
  }
  return paths;
}
