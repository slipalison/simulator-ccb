import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { RegistrationForm } from "@/components/molecules/RegistrationForm";

// Mock API
vi.mock("@/lib/api", () => ({
  registerCompany: vi.fn(),
  RegistrationValidationError: class extends Error {
    constructor(public errors: Record<string, string[]>) {
      super("Validation failed");
      this.name = "RegistrationValidationError";
    }
  },
  DuplicateClientError: class extends Error {
    constructor(message: string) { super(message); this.name = "DuplicateClientError"; }
  },
  RegistrationUnavailable: class extends Error {
    constructor(message: string) { super(message); this.name = "RegistrationUnavailable"; }
  },
  ApiError: class extends Error {
    constructor(message: string) { super(message); this.name = "ApiError"; }
  },
}));

// Mock auth context — ACF version (PJ-only)
const mockLogin = vi.fn();
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    login: mockLogin,
    auth: { isAuthenticated: false, isLoading: false, userName: null, email: null, accessGroup: null, companyId: null },
    logout: vi.fn(),
  }),
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}));

// Mock router navigation
const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("RegistrationForm (PJ-only)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders PJ-only form with email, phone, password fields", () => {
    render(<RegistrationForm />);

    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByLabelText("Telefone")).toBeInTheDocument();
    expect(screen.getByLabelText("Senha")).toBeInTheDocument();
    expect(screen.getByLabelText("Confirmar senha")).toBeInTheDocument();
  });

  it("renders terms acceptance checkbox", () => {
    render(<RegistrationForm />);

    expect(screen.getByText(/aceito os termos de uso/i)).toBeInTheDocument();
  });

  it("renders PJ-only notice", () => {
    render(<RegistrationForm />);

    expect(screen.getByText(/pessoa jurídica/i)).toBeInTheDocument();
  });

  it("renders password strength meter", async () => {
    render(<RegistrationForm />);

    // Type a strong password
    await act(async () => {
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Abcdefg1!xyz" } });
    });

    await waitFor(() => {
      expect(screen.getByText(/muito forte/i)).toBeInTheDocument();
    });
  });

  it('shows "Fazer login" link', () => {
    render(<RegistrationForm />);

    const loginLink = screen.getByRole("link", { name: /fazer login/i });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/auth/login");
  });
});