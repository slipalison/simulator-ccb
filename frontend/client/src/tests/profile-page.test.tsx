// ---------------------------------------------------------------------------
// GREEN tests — ProfilePage integration tests — ACF version (PJ-only)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { AuthProvider } from "@/lib/auth-context";
import { router } from "@/router";
import type { CompanyProfileDto } from "@/lib/types";

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
const mockUseAuth = vi.fn();

vi.mock("@/lib/auth-context", () => ({
  useAuth: () => mockUseAuth(),
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}));

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

const mockCompanyProfile: CompanyProfileDto = {
  id: "company-id-456",
  razaoSocial: "Empresa LTDA",
  cnpj: "12345678000190",
  email: "contato@empresa.com.br",
  phone: "11999990000",
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
    // Default: authenticated user with admin-empresa group
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: true, isLoading: false, userName: "Test User", email: "test@example.com", accessGroup: "admin-empresa", companyId: "company-123" },
      login: vi.fn(),
      logout: mockLogout,
    });
  });

  it("redirects to /auth/login when not authenticated", async () => {
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: false, isLoading: false, userName: null, email: null, accessGroup: null, companyId: null },
      login: vi.fn(),
      logout: mockLogout,
    });

    await renderProfilePage("/profile");

    // Profile data should NOT be visible
    await waitFor(() => {
      expect(screen.queryByText("Perfil da Empresa")).not.toBeInTheDocument();
    });
  });

  it("renders company profile data successfully", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockCompanyProfile);

    await renderProfilePage("/profile");

    await waitFor(() => {
      expect(screen.getByText("Empresa LTDA")).toBeInTheDocument();
    });

    expect(screen.getByText("12345678000190")).toBeInTheDocument();
    expect(screen.getByText("contato@empresa.com.br")).toBeInTheDocument();
  });
});