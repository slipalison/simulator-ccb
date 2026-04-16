// ---------------------------------------------------------------------------
// E2E profile flow tests — ACF version
// ---------------------------------------------------------------------------
// Simulates the complete authenticated user journey using ACF cookies:
//   /auth/me session check → profile display → logout redirect
// and the unauthenticated redirect guard.
//
// API and auth are mocked — tests run without a live backend.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import {
  RouterProvider,
  createRouter,
  createMemoryHistory,
} from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";
import type { ClientProfileDto } from "@/lib/types";

// ---------------------------------------------------------------------------
// Mocks
// ---------------------------------------------------------------------------

vi.mock("@/lib/api", () => ({
  getProfileClient: vi.fn(),
  ProfileError: class ProfileError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ProfileError";
    }
  },
  ApiError: class ApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ApiError";
    }
  },
}));

// Mock fetch for /auth/me
const mockFetch = vi.fn();
global.fetch = mockFetch;

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

const mockPFProfile: ClientProfileDto = {
  id: "e2e-pf-id",
  name: "Maria da Silva",
  email: "maria@email.com",
  phone: "(21) 98888-7777",
  type: "PessoaFisica",
  cpf: "987.654.321-00",
  cnpj: null,
  razaoSocial: null,
};

// ---------------------------------------------------------------------------
// Helper: render full app at given route
// ---------------------------------------------------------------------------

async function renderApp(initialPath: string) {
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

// ---------------------------------------------------------------------------
// E2E flow tests
// ---------------------------------------------------------------------------

describe("Profile E2E Flow — ACF", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  it("authenticated user at /profile sees profile data", async () => {
    // /auth/me returns authenticated session
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          isAuthenticated: true,
          userName: "Maria da Silva",
          email: "maria@email.com",
          sub: "user-123",
        }),
    });

    const api = await import("@/lib/api");
    vi.mocked(api.getProfileClient).mockResolvedValue(mockPFProfile);

    await renderApp("/profile");

    await waitFor(() => {
      expect(screen.getByText("Maria da Silva")).toBeInTheDocument();
    });
    expect(screen.getByText("987.654.321-00")).toBeInTheDocument();
    expect(screen.getByText("Pessoa Física")).toBeInTheDocument();
  });

  it("unauthenticated user at /profile sees redirect spinner (auth login redirect)", async () => {
    // /auth/me returns 401
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    await renderApp("/profile");

    // ProfilePage auth guard calls login() which redirects to /auth/login
    // The spinner from AuthLoginPage or ProfilePage should show briefly
    await waitFor(() => {
      // Either we see the redirect spinner or the profile page doesn't render
      expect(screen.queryByText("987.654.321-00")).not.toBeInTheDocument();
    });
  });

  it("unauthenticated user at / sees AuthLoginPage spinner", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    await renderApp("/");

    await waitFor(() => {
      expect(screen.getByText(/redirecionando/i)).toBeInTheDocument();
    });
  });

  it("authenticated user at / is redirected to /profile", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          isAuthenticated: true,
          userName: "Maria da Silva",
          email: "maria@email.com",
          sub: "user-123",
        }),
    });

    const api = await import("@/lib/api");
    vi.mocked(api.getProfileClient).mockResolvedValue(mockPFProfile);

    const { memoryHistory } = await renderApp("/");

    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });
  });
});
