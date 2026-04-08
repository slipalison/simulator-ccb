// ---------------------------------------------------------------------------
// GREEN tests — ProfilePage integration tests
// ---------------------------------------------------------------------------
// Implemented in plan 10-03: turned all RED stubs to GREEN.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  render,
  screen,
  waitFor,
  act,
  fireEvent,
} from "@testing-library/react";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";
import type { ClientProfileDto } from "@/lib/types";

// ---------------------------------------------------------------------------
// Mock the API client
// ---------------------------------------------------------------------------

vi.mock("@/lib/api", () => ({
  getProfileClient: vi.fn(),
  ProfileError: class ProfileError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ProfileError";
    }
  },
  loginClient: vi.fn(),
  refreshTokenClient: vi.fn(),
}));

// ---------------------------------------------------------------------------
// Mock auth context — mirrors actual shape: { auth, login, logout, refreshIfNeeded, getAccessToken }
// We use vi.fn() so individual tests can override via mockReturnValue / mockImplementation.
// ---------------------------------------------------------------------------

const mockLogout = vi.fn();
const mockUseAuth = vi.fn(() => ({
  auth: { isAuthenticated: true, isLoading: false },
  login: vi.fn(),
  logout: mockLogout,
  refreshIfNeeded: vi.fn(),
  getAccessToken: () => "fake-token",
}));

vi.mock("@/lib/auth-context", () => ({
  useAuth: () => mockUseAuth(),
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
  getAccessToken: () => "fake-token",
}));

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

const mockPFProfile: ClientProfileDto = {
  id: "pf-id-123",
  name: "João da Silva",
  email: "joao@email.com",
  phone: "(11) 99999-9999",
  type: "PessoaFisica",
  cpf: "123.456.789-00",
  cnpj: null,
  razaoSocial: null,
};

const mockPJProfile: ClientProfileDto = {
  id: "pj-id-456",
  name: "Empresa LTDA",
  email: "contato@empresa.com.br",
  phone: "(11) 3333-4444",
  type: "PessoaJuridica",
  cpf: null,
  cnpj: "12.345.678/0001-90",
  razaoSocial: "Empresa LTDA",
};

// ---------------------------------------------------------------------------
// Helper: render ProfilePage inside router context
// ---------------------------------------------------------------------------

async function renderProfilePage(initialPath = "/profile") {
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
// ProfilePage integration tests
// ---------------------------------------------------------------------------

describe("ProfilePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Default: authenticated user
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: true, isLoading: false },
      login: vi.fn(),
      logout: mockLogout,
      refreshIfNeeded: vi.fn(),
      getAccessToken: () => "fake-token",
    });
  });

  it("redirects to /login when not authenticated", async () => {
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: false, isLoading: false },
      login: vi.fn(),
      logout: mockLogout,
      refreshIfNeeded: vi.fn(),
      getAccessToken: () => null,
    });

    const { memoryHistory } = await renderProfilePage("/profile");

    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/login");
    });
  });

  it("shows loading state initially", async () => {
    // getProfileClient never resolves — stays loading
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockImplementation(
      () => new Promise(() => {})
    );

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByTestId("profile-loading")).toBeInTheDocument();
    });
  });

  it("shows error state when API call fails", async () => {
    const { getProfileClient, ProfileError } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockRejectedValue(
      new ProfileError("Authentication required")
    );

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByTestId("profile-error")).toBeInTheDocument();
    });
  });

  it("renders PF profile data successfully", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPFProfile);

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByText("João da Silva")).toBeInTheDocument();
    });

    expect(screen.getByText("123.456.789-00")).toBeInTheDocument();
    expect(screen.getByText("joao@email.com")).toBeInTheDocument();
    expect(screen.getByText("Pessoa Física")).toBeInTheDocument();
  });

  it("renders PJ profile data successfully", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPJProfile);

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByText("Empresa LTDA")).toBeInTheDocument();
    });

    expect(screen.getByText("12.345.678/0001-90")).toBeInTheDocument();
    expect(screen.getByText("contato@empresa.com.br")).toBeInTheDocument();
    expect(screen.getByText("Pessoa Jurídica")).toBeInTheDocument();
  });

  it("does not show CPF field for PJ profile", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPJProfile);

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByText("Empresa LTDA")).toBeInTheDocument();
    });

    expect(screen.queryByText("CPF")).not.toBeInTheDocument();
  });

  it("does not show CNPJ field for PF profile", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPFProfile);

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByText("João da Silva")).toBeInTheDocument();
    });

    expect(screen.queryByText("CNPJ")).not.toBeInTheDocument();
  });

  it("calls logout when Sair button is clicked", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPFProfile);

    await renderProfilePage("/profile");

    // Wait for profile to load and Sair button to appear
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /sair/i })).toBeInTheDocument();
    });

    // Click logout button
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /sair/i }));
    });

    // logout() should have been called — this is the critical behavior
    expect(mockLogout).toHaveBeenCalledOnce();
  });
});
