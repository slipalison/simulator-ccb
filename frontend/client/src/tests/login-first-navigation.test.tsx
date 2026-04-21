import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";

// Mock fetch for /auth/me
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock auth context to control auth state
const mockAuthState = {
  isAuthenticated: false,
  isLoading: false,
  userName: null as string | null,
  email: null as string | null,
};

vi.mock("@/lib/auth-context", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/auth-context")>();
  return {
    ...actual,
    useAuth: () => ({
      auth: mockAuthState,
      login: vi.fn(() => { window.location.href = "/auth/login"; }),
      logout: vi.fn(() => { window.location.href = "/auth/logout"; }),
    }),
  };
});

async function renderWithRouter(initialPath: string) {
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

describe("Login-first navigation — ACF", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
    mockAuthState.isAuthenticated = false;
    mockAuthState.isLoading = false;
    mockAuthState.userName = null;
    mockAuthState.email = null;
  });

  it("shows AuthLoginPage (redirect spinner) for unauthenticated user at /", async () => {
    await renderWithRouter("/");

    await waitFor(() => {
      expect(screen.getByText(/redirecionando/i)).toBeInTheDocument();
    });
  });

  it("redirects to /profile if authenticated user visits /", async () => {
    mockAuthState.isAuthenticated = true;

    const { memoryHistory } = await renderWithRouter("/");

    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });
  });

  it("shows AuthCallbackPage spinner at /auth/callback", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    await renderWithRouter("/auth/callback");

    await waitFor(() => {
      expect(screen.getByText(/concluindo/i)).toBeInTheDocument();
    });
  });

  it("shows AuthErrorPage at /auth/error", async () => {
    Object.defineProperty(window, "location", {
      value: { search: "", href: "/auth/error" },
      writable: true,
    });

    await renderWithRouter("/auth/error");

    await waitFor(() => {
      expect(screen.getByText(/erro de autenticação/i)).toBeInTheDocument();
    });
  });
});
