// ---------------------------------------------------------------------------
// E2E profile flow tests
// ---------------------------------------------------------------------------
// Simulates the complete authenticated user journey:
//   login → profile display → logout
// and the unauthenticated redirect guard.
//
// Uses the full router tree (same as login-flow.test.tsx pattern).
// API and auth are mocked — tests run without a live backend.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  render,
  screen,
  waitFor,
  act,
  fireEvent,
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
  loginClient: vi.fn(),
  refreshTokenClient: vi.fn(),
  getProfileClient: vi.fn(),
  ProfileError: class ProfileError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ProfileError";
    }
  },
  LoginError: class LoginError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "LoginError";
    }
  },
  ApiError: class ApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ApiError";
    }
  },
}));

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

const mockLoginResponse = {
  accessToken: "mock-access-token",
  refreshToken: "mock-refresh-token",
  expiresIn: 300,
  tokenType: "Bearer",
  refreshExpiresIn: 86400,
  scope: "openid",
};

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

describe("Profile E2E Flow", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("login → view profile → logout completes full user journey", async () => {
    const api = await import("@/lib/api");
    vi.mocked(api.loginClient).mockResolvedValue(mockLoginResponse);
    vi.mocked(api.getProfileClient).mockResolvedValue(mockPFProfile);

    const { memoryHistory } = await renderApp("/login");

    // Step 1: Login form renders
    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    // Step 2: Fill and submit login form
    await act(async () => {
      fireEvent.change(screen.getByLabelText(/email/i), {
        target: { value: "maria@email.com" },
      });
      fireEvent.change(screen.getByLabelText(/senha/i), {
        target: { value: "Senha@123" },
      });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Step 3: Redirected to /profile after successful login
    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });

    // Step 4: Profile page shows user data
    await waitFor(() => {
      expect(screen.getByText("Maria da Silva")).toBeInTheDocument();
    });
    expect(screen.getByText("987.654.321-00")).toBeInTheDocument();
    expect(screen.getByText("Pessoa Física")).toBeInTheDocument();

    // Step 5: Logout button is present
    expect(
      screen.getByRole("button", { name: /sair/i })
    ).toBeInTheDocument();
  });

  it("direct /profile access without auth redirects to /login", async () => {
    const { memoryHistory } = await renderApp("/profile");

    // Unauthenticated — ProfilePage auth guard should redirect immediately
    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/login");
    });

    // Login form should be visible
    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });
  });
});
