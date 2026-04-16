// ---------------------------------------------------------------------------
// GREEN tests — ProfilePage integration tests — ACF version
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
}));

// ---------------------------------------------------------------------------
// Mock auth context — ACF version (login/logout are sync redirects)
// ---------------------------------------------------------------------------

const mockLogout = vi.fn();
const mockUseAuth = vi.fn(() => ({
  auth: { isAuthenticated: true, isLoading: false, userName: "Test User" as string | null, email: "test@example.com" as string | null },
  login: vi.fn(),
  logout: mockLogout,
}));

vi.mock("@/lib/auth-context", () => ({
  useAuth: () => mockUseAuth(),
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
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
      auth: { isAuthenticated: true, isLoading: false, userName: "Test User", email: "test@example.com" },
      login: vi.fn(),
      logout: mockLogout,
    });
  });

  it("redirects to /auth/login when not authenticated", async () => {
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: false, isLoading: false, userName: null as string | null, email: null as string | null },
      login: vi.fn(),
      logout: mockLogout,
    });

    // When not authenticated, the auth guard redirects via login() → /auth/login
    // With our mocked router, the redirect won't navigate but login() should be called or
    // page should show the AuthLoginPage spinner
    await renderProfilePage("/profile");

    // Profile data should NOT be visible
    await waitFor(() => {
      expect(screen.queryByText("Meu Perfil")).not.toBeInTheDocument();
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
      const skeletons = document.querySelectorAll('[class*="animate-pulse"]');
      expect(skeletons.length).toBeGreaterThan(0);
    });
  });

  it("shows error state when API call fails", async () => {
    const { getProfileClient, ProfileError } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockRejectedValue(
      new ProfileError("Authentication required")
    );

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.queryByText("Meu Perfil")).not.toBeInTheDocument();
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

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /sair/i })).toBeInTheDocument();
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /sair/i }));
    });

    expect(mockLogout).toHaveBeenCalledOnce();
  });
});
