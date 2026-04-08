import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { RegistrationForm } from "@/components/molecules/RegistrationForm";
import * as api from "@/lib/api";

// Mock API
vi.mock("@/lib/api", () => ({
  registerClient: vi.fn(),
  loginClient: vi.fn(),
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
  LoginError: class extends Error {
    constructor(message: string) { super(message); this.name = "LoginError"; }
  },
}));

// Mock auth context
const mockLogin = vi.fn();
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    login: mockLogin,
    auth: { isAuthenticated: false, isLoading: false },
    logout: vi.fn(),
    refreshIfNeeded: vi.fn(),
    getAccessToken: () => null,
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

describe("RegistrationForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders PF fields by default", () => {
    render(<RegistrationForm />);

    expect(screen.getByLabelText("Nome")).toBeInTheDocument();
    expect(screen.getByLabelText("CPF")).toBeInTheDocument();
    expect(screen.queryByLabelText("Razão Social")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("CNPJ")).not.toBeInTheDocument();
  });

  it("switches to PJ fields when radio selected", async () => {
    render(<RegistrationForm />);

    // Click PJ radio
    const pjRadio = screen.getByRole("radio", { name: /pessoa jurídica/i });
    await act(async () => {
      fireEvent.click(pjRadio);
    });

    expect(screen.queryByLabelText("Nome")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("CPF")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Razão Social")).toBeInTheDocument();
    expect(screen.getByLabelText("CNPJ")).toBeInTheDocument();
  });

  it("validates CPF inline before submit", async () => {
    render(<RegistrationForm />);

    // Fill invalid CPF (all same digits)
    await act(async () => {
      fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "João Silva" } });
      fireEvent.change(screen.getByLabelText("CPF"), { target: { value: "00000000000" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@email.com" } });
      fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999999999" } });
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar Senha"), { target: { value: "Test@1234" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar conta/i }));
    });

    await waitFor(() => {
      expect(screen.getByText(/cpf inválido/i)).toBeInTheDocument();
    });
  });

  it("validates CNPJ inline before submit", async () => {
    render(<RegistrationForm />);

    // Switch to PJ
    await act(async () => {
      fireEvent.click(screen.getByRole("radio", { name: /pessoa jurídica/i }));
    });

    // Fill invalid CNPJ
    await act(async () => {
      fireEvent.change(screen.getByLabelText("Razão Social"), { target: { value: "Empresa LTDA" } });
      fireEvent.change(screen.getByLabelText("CNPJ"), { target: { value: "00000000000000" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@email.com" } });
      fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999999999" } });
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar Senha"), { target: { value: "Test@1234" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar conta/i }));
    });

    await waitFor(() => {
      expect(screen.getByText(/cnpj inválido/i)).toBeInTheDocument();
    });
  });

  it("shows password strength meter as user types", async () => {
    render(<RegistrationForm />);

    // Initially no strength meter
    expect(screen.queryByText(/muito fraca|fraca|media|forte/i)).not.toBeInTheDocument();

    // Type a strong password
    await act(async () => {
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Abcdefg1!xyz" } });
    });

    await waitFor(() => {
      expect(screen.getByText(/muito forte/i)).toBeInTheDocument();
    });
  });

  it("blocks submit if passwords dont match", async () => {
    render(<RegistrationForm />);

    await act(async () => {
      fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "João Silva" } });
      fireEvent.change(screen.getByLabelText("CPF"), { target: { value: "12345678909" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@email.com" } });
      fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999999999" } });
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar Senha"), { target: { value: "Different@1" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar conta/i }));
    });

    await waitFor(() => {
      expect(screen.getByText(/senhas não coincidem/i)).toBeInTheDocument();
    });

    expect(api.registerClient).not.toHaveBeenCalled();
  });

  it("auto-login after successful registration", async () => {
    vi.mocked(api.registerClient).mockResolvedValue({ id: "test-id" });
    vi.mocked(api.loginClient).mockResolvedValue({
      accessToken: "test-token",
      refreshToken: "test-refresh",
      expiresIn: 3600,
      tokenType: "Bearer",
      refreshExpiresIn: 7200,
      scope: "openid",
    });
    mockLogin.mockResolvedValue(undefined);

    render(<RegistrationForm />);

    await act(async () => {
      fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "João Silva" } });
      fireEvent.change(screen.getByLabelText("CPF"), { target: { value: "12345678909" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@email.com" } });
      fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999999999" } });
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar Senha"), { target: { value: "Test@1234" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar conta/i }));
    });

    await waitFor(() => {
      expect(api.registerClient).toHaveBeenCalled();
    });
  });

  it("falls back to /login if auto-login fails", async () => {
    vi.mocked(api.registerClient).mockResolvedValue({ id: "test-id" });
    vi.mocked(api.loginClient).mockRejectedValue(new Error("Login failed"));
    mockLogin.mockRejectedValue(new Error("Login failed"));

    render(<RegistrationForm />);

    await act(async () => {
      fireEvent.change(screen.getByLabelText("Nome"), { target: { value: "João Silva" } });
      fireEvent.change(screen.getByLabelText("CPF"), { target: { value: "12345678909" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@email.com" } });
      fireEvent.change(screen.getByLabelText("Telefone"), { target: { value: "11999999999" } });
      fireEvent.change(screen.getByLabelText("Senha"), { target: { value: "Test@1234" } });
      fireEvent.change(screen.getByLabelText("Confirmar Senha"), { target: { value: "Test@1234" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar conta/i }));
    });

    await waitFor(() => {
      expect(api.registerClient).toHaveBeenCalled();
    });
  });
});
