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

function renderRegistrationForm() {
  return render(<RegistrationForm />);
}

describe("RegistrationForm (shadcn redesign — PJ-only)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with shadcn Card container", () => {
    renderRegistrationForm();

    // Card header should be visible
    expect(screen.getByText("Criar sua conta")).toBeInTheDocument();
  });

  it("shows PJ-only notice", () => {
    renderRegistrationForm();

    expect(screen.getByText(/pessoa jurídica/i)).toBeInTheDocument();
  });

  it("renders email and phone fields", () => {
    renderRegistrationForm();

    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByLabelText("Telefone")).toBeInTheDocument();
  });

  it("renders password and confirm password fields", () => {
    renderRegistrationForm();

    expect(screen.getByLabelText("Senha")).toBeInTheDocument();
    expect(screen.getByLabelText("Confirmar senha")).toBeInTheDocument();
  });

  it("renders terms acceptance checkbox", () => {
    renderRegistrationForm();

    expect(screen.getByText(/aceito os termos de uso/i)).toBeInTheDocument();
  });

  it("renders password strength meter", async () => {
    renderRegistrationForm();

    const passwordInput = screen.getByLabelText("Senha");
    expect(passwordInput).toBeInTheDocument();

    // Type a strong password
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: "Abcdefg1!xyz" } });
    });

    // Strength meter should appear
    await waitFor(() => {
      expect(screen.getByText(/muito forte/i)).toBeInTheDocument();
    });
  });

  it('shows "Fazer login" link', () => {
    renderRegistrationForm();

    const loginLink = screen.getByRole("link", { name: /Fazer login/ });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/auth/login");
  });
});