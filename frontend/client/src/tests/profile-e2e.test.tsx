// ---------------------------------------------------------------------------
// E2E profile flow tests (ACF-based)
// ---------------------------------------------------------------------------
// Tests the profile display with authenticated session (cookie-based).
// Login is via redirect to Keycloak — we simulate authenticated state directly.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  render,
  screen,
  waitFor,
  act,
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

// Mock fetch for auth context session restoration
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock window.location
const originalLocation = window.location;
beforeEach(() => {
  vi.clearAllMocks();
  mockFetch.mockReset();
  Object.defineProperty(window, "location", {
    writable: true,
    value: { href: "" },
  });
});
afterEach(() => {
  Object.defineProperty(window, "location", {
    writable: true,
    value: originalLocation,
  });
});

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

async function renderApp(initialPath: string, isAuthenticated = false) {
  const memoryHistory = createMemoryHistory({ initialEntries: [initialPath] });
  const testRouter = createRouter({
    routeTree: router.options.routeTree,
    history: memoryHistory,
  });

  if (isAuthenticated) {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          userName: "Maria da Silva",
          email: "maria@email.com",
          isAuthenticated: true,
        }),
    });
  } else {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });
  }

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

describe("Profile E2E Flow (ACF)", () => {
  it("authenticated user can view profile", async () => {
    const api = await import("@/lib/api");
    vi.mocked(api.getProfileClient).mockResolvedValue(mockPFProfile);

    const { memoryHistory } = await renderApp("/profile", true);

    // Profile page should show user data
    await waitFor(() => {
      expect(screen.getByText("Maria da Silva")).toBeInTheDocument();
    });
    expect(screen.getByText("987.654.321-00")).toBeInTheDocument();
    expect(screen.getByText("Pessoa Física")).toBeInTheDocument();
  });

  it("unauthenticated user cannot view profile", async () => {
    await renderApp("/profile", false);

    // Unauthenticated — profile should not show user data
    await waitFor(() => {
      expect(screen.queryByText("Maria da Silva")).not.toBeInTheDocument();
    });
  });

  it("logout redirects to /auth/logout", async () => {
    const api = await import("@/lib/api");
    vi.mocked(api.getProfileClient).mockResolvedValue(mockPFProfile);

    await renderApp("/profile", true);

    // Wait for profile to load
    await waitFor(() => {
      expect(screen.getByText("Maria da Silva")).toBeInTheDocument();
    });

    // Find and click logout button
    const logoutButton = screen.getByRole("button", { name: /sair/i });
    await act(async () => {
      logoutButton.click();
    });

    expect(window.location.href).toBe("/auth/logout");
  });
});
