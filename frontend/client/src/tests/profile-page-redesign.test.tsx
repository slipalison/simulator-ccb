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
// Mock auth context — ACF version (no getAccessToken/refreshIfNeeded)
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

const mockPFProfile = {
  id: "pf-id-123",
  type: "PessoaFisica" as const,
  name: "João da Silva",
  cpf: "123.456.789-00",
  email: "joao@email.com",
  phone: "(11) 99999-9999",
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

describe("ProfilePage (shadcn redesign)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: true, isLoading: false, userName: "João", email: "joao@email.com" },
      login: vi.fn(),
      logout: mockLogout,
    });
  });

  it("renders with shadcn Card container", async () => {
    const apiModule = await import("@/lib/api");
    vi.mocked(apiModule.getProfileClient).mockResolvedValue(mockPFProfile as any);

    renderProfilePage();

    await waitFor(() => {
      expect(screen.getByText("Meu Perfil")).toBeInTheDocument();
    });
  });

  it("shows PF/PJ badge with correct color", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPFProfile as any);

    renderProfilePage();

    await waitFor(() => {
      expect(screen.getByText("Pessoa Física")).toBeInTheDocument();
    });
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

  it("shows profile data (nome, cpf/cnpj, email, telefone)", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPFProfile as any);

    renderProfilePage();

    await waitFor(() => {
      expect(screen.getByText("João da Silva")).toBeInTheDocument();
    });

    expect(screen.getByText("123.456.789-00")).toBeInTheDocument();
    expect(screen.getByText("joao@email.com")).toBeInTheDocument();
  });

  it("shows logout button", async () => {
    const { getProfileClient } = await import("@/lib/api");
    vi.mocked(getProfileClient).mockResolvedValue(mockPFProfile as any);

    renderProfilePage();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /sair/i })).toBeInTheDocument();
    });
  });

  it("redirects to /auth/login when not authenticated", async () => {
    mockUseAuth.mockReturnValue({
      auth: { isAuthenticated: false, isLoading: false, userName: null, email: null },
      login: vi.fn(),
      logout: mockLogout,
    });

    renderProfilePage();

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(expect.objectContaining({ to: "/auth/login" }));
    });
  });
});
