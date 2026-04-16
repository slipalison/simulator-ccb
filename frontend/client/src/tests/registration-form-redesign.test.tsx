import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { RegistrationForm } from "@/components/molecules/RegistrationForm";
import * as api from "@/lib/api";

// Mock API
vi.mock("@/lib/api", () => ({
  registerClient: vi.fn(),
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

// Mock auth context — ACF version
const mockLogin = vi.fn();
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    login: mockLogin,
    auth: { isAuthenticated: false, isLoading: false, userName: null, email: null },
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
  return render(
    <RegistrationForm />
  );
}

describe("RegistrationForm (shadcn redesign)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with shadcn Card container", () => {
    renderRegistrationForm();

    // Card header should be visible
    expect(screen.getByText("Criar sua conta")).toBeInTheDocument();
  });

  it("renders PF/PJ radio group with shadcn styling", () => {
    renderRegistrationForm();

    // Radio buttons should exist
    const pfRadio = screen.getByRole("radio", { name: /pessoa f.sica/i });
    const pjRadio = screen.getByRole("radio", { name: /pessoa jur.dica/i });

    expect(pfRadio).toBeInTheDocument();
    expect(pjRadio).toBeInTheDocument();
  });

  it("shows PF fields (nome, cpf) when PF selected", () => {
    renderRegistrationForm();

    expect(screen.getByLabelText("Nome completo")).toBeInTheDocument();
    expect(screen.getByLabelText("CPF")).toBeInTheDocument();
    expect(screen.queryByLabelText("Razão Social")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("CNPJ")).not.toBeInTheDocument();
  });

  it("shows PJ fields (razaoSocial, cnpj) when PJ selected", async () => {
    renderRegistrationForm();

    // Click PJ radio
    const pjRadio = screen.getByRole("radio", { name: /pessoa jur.dica/i });
    await act(async () => {
      fireEvent.click(pjRadio);
    });

    expect(screen.queryByLabelText("Nome completo")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("CPF")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Razão Social")).toBeInTheDocument();
    expect(screen.getByLabelText("CNPJ")).toBeInTheDocument();
  });

  it("renders password field with strength meter", async () => {
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

  it("renders confirm password field", () => {
    renderRegistrationForm();

    const confirmInput = screen.getByLabelText("Confirmar senha");
    expect(confirmInput).toBeInTheDocument();
  });

  it("shows password match indicator when passwords match", async () => {
    renderRegistrationForm();

    // Fill both password fields
    await act(async () => {
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar senha"), { target: { value: "Test@1234" } });
    });

    await waitFor(() => {
      expect(screen.getByText(/senhas coincidem/i)).toBeInTheDocument();
    });
  });

  it("shows loading spinner when submitting", async () => {
    // Make register hang so we can see loading state
    vi.mocked(api.registerClient).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 5000))
    );

    renderRegistrationForm();

    // Fill required PF fields
    await act(async () => {
      fireEvent.change(screen.getByLabelText("Nome completo"), { target: { value: "Joao Silva" } });
      fireEvent.change(screen.getByLabelText("CPF"), { target: { value: "12345678909" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@email.com" } });
      fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999999999" } });
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar senha"), { target: { value: "Test@1234" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar conta/i }));
    });

    // Button should be disabled during submit
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /criar conta/i })).toBeDisabled();
    });
  });

  it('shows "Fazer login" link', () => {
    renderRegistrationForm();

    const loginLink = screen.getByRole("link", { name: /Fazer login/ });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/auth/login");
  });
});
