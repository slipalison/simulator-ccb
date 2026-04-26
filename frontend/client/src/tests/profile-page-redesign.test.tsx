import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { ProfilePage } from "@/components/pages/ProfilePage";

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
// Mock auth context — ACF version (PJ-only, includes accessGroup)
// ---------------------------------------------------------------------------

const mockLogout = vi.fn();
const mockUseAuth = vi.fn();

vi.mock("@/lib/auth-context", () => ({
  useAuth: () => mockUseAuth(),
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}));

// ---------------------------------------------------------------------------
// Mock router
// ---------------------------------------------------------------------------

const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

const mockCompanyProfile = {
  id: "company-id-456",
  razaoSocial: "Empresa LTDA",
  cnpj: "12345678000190",
  email: "contato@empresa.com.br",
  phone: "11999990000",
};

// ---------------------------------------------------------------------------
// Helper
// ---------------------------------------------------------------------------

function renderProfilePage() {
  return render(<ProfilePage />);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe("ProfilePage (PJ-only redesign)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: true, isLoading: false, userName: "Test User", email: "test@company.com", accessGroup: "admin-empresa", companyId: "company-123" },
      login: vi.fn(),
      logout: mockLogout,
    });
  });

  it("renders company profile data with shadcn Card container", async () => {
    const apiModule = await import("@/lib/api");
    vi.mocked(apiModule.getProfileClient).mockResolvedValue(mockCompanyProfile as any);

    renderProfilePage();

    await waitFor(() => {
      expect(screen.getByText("Perfil da Empresa")).toBeInTheDocument();
    });

    expect(screen.getByText("Empresa LTDA")).toBeInTheDocument();
    expect(screen.getByText("12345678000190")).toBeInTheDocument();
  });

  it("shows skeleton loading state", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockImplementation(() => new Promise(() => {}));

    renderProfilePage();

    await waitFor(() => {
      const skeletons = document.querySelectorAll('[class*="animate-pulse"]');
      expect(skeletons.length).toBeGreaterThan(0);
    });
  });

  it("redirects to /auth/login when not authenticated", async () => {
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: false, isLoading: false, userName: null, email: null, accessGroup: null, companyId: null },
      login: vi.fn(),
      logout: mockLogout,
    });

    renderProfilePage();

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(expect.objectContaining({ to: "/auth/login" }));
    });
  });
});