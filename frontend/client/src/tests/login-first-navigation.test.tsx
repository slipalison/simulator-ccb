import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, act } from "@testing-library/react";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";

// Mock API
vi.mock("@/lib/api", () => ({
  loginClient: vi.fn(),
  refreshTokenClient: vi.fn(),
  getProfileClient: vi.fn(),
  LoginError: class extends Error {
    constructor(message: string) { super(message); this.name = "LoginError"; }
  },
  ProfileError: class extends Error {
    constructor(message: string) { super(message); this.name = "ProfileError"; }
  },
}));

// Mock auth context
const mockAuthState = {
  isAuthenticated: false,
  isLoading: false,
};

vi.mock("@/lib/auth-context", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/auth-context")>();
  return {
    ...actual,
    useAuth: () => ({
      auth: mockAuthState,
      login: vi.fn(),
      logout: vi.fn(),
      refreshIfNeeded: vi.fn(),
      getAccessToken: () => mockAuthState.isAuthenticated ? "fake-token" : null,
    }),
    getAccessToken: () => mockAuthState.isAuthenticated ? "fake-token" : null,
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

describe("Login-first navigation", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockAuthState.isAuthenticated = false;
    mockAuthState.isLoading = false;
  });

  it("shows LoginPage for unauthenticated user at /", async () => {
    await renderWithRouter("/");

    await waitFor(() => {
      expect(screen.getByText("Bem-vindo de volta!")).toBeInTheDocument();
    });
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByLabelText("Senha")).toBeInTheDocument();
  });

  it("redirects to /profile if authenticated user visits /", async () => {
    mockAuthState.isAuthenticated = true;

    const { memoryHistory } = await renderWithRouter("/");

    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });
  });

  it("redirects to /profile if authenticated user visits /login", async () => {
    mockAuthState.isAuthenticated = true;

    const { memoryHistory } = await renderWithRouter("/login");

    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });
  });
});
