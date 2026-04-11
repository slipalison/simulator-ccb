import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { LoginPage } from "@/components/pages/LoginPage";
import { AuthProvider } from "@/lib/auth-context";
import * as api from "@/lib/api";

// Mock API
vi.mock("@/lib/api", () => ({
  loginClient: vi.fn(),
  LoginError: class extends Error {
    constructor(message: string) { super(message); this.name = "LoginError"; }
  },
  ApiError: class extends Error {
    constructor(message: string) { super(message); this.name = "ApiError"; }
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

// Mock router
const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

function renderLoginPage() {
  return render(
    <AuthProvider>
      <LoginPage />
    </AuthProvider>
  );
}

describe("LoginForm (shadcn redesign)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with shadcn Card container", () => {
    renderLoginPage();

    // Card should have the header text centered
    expect(screen.getByText("Bem-vindo de volta!")).toBeInTheDocument();
  });

  it("renders email input with shadcn Input", () => {
    renderLoginPage();

    const emailInput = screen.getByLabelText(/email/i);
    expect(emailInput).toBeInTheDocument();
    expect(emailInput).toHaveAttribute("placeholder", "seu@email.com");
  });

  it("renders password field with show/hide toggle", () => {
    renderLoginPage();

    // Password input should exist (use getAllByLabelText since there are multiple "senha" references)
    const passwordInputs = screen.getAllByLabelText(/senha/i);
    const passwordInput = passwordInputs.find(el => el.tagName === "INPUT") as HTMLInputElement;
    expect(passwordInput).toBeInTheDocument();

    // Toggle button should exist
    const toggleButton = screen.getByRole("button", { name: /mostrar senha/i });
    expect(toggleButton).toBeInTheDocument();
  });

  it("renders submit button with text 'Entrar'", () => {
    renderLoginPage();

    const submitButton = screen.getByRole("button", { name: /entrar/i });
    expect(submitButton).toBeInTheDocument();
  });

  it("shows loading spinner when submitting", async () => {
    // Make login hang so we can see the loading state
    mockLogin.mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 5000))
    );

    renderLoginPage();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    const emailInput = screen.getByLabelText(/email/i);
    const passwordInputs = screen.getAllByLabelText(/senha/i);
    const passwordInput = passwordInputs.find(el => el.tagName === "INPUT") as HTMLInputElement;

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "test@example.com" } });
      fireEvent.change(passwordInput, { target: { value: "password123" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Button should be disabled during submit
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeDisabled();
    });
  });

  it("shows error alert on invalid credentials", async () => {
    mockLogin.mockRejectedValue(new Error("Invalid credentials"));

    renderLoginPage();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    const emailInput = screen.getByLabelText(/email/i);
    const passwordInputs = screen.getAllByLabelText(/senha/i);
    const passwordInput = passwordInputs.find(el => el.tagName === "INPUT") as HTMLInputElement;

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "wrong@example.com" } });
      fireEvent.change(passwordInput, { target: { value: "wrongpassword" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Error message should appear
    await waitFor(() => {
      const alert = screen.getByRole("alert");
      expect(alert).toBeInTheDocument();
    });
  });

  it('shows "Esqueceu a senha?" link', () => {
    renderLoginPage();

    const forgotLink = screen.getByRole("link", { name: /esqueceu a senha/i });
    expect(forgotLink).toBeInTheDocument();
    expect(forgotLink).toHaveAttribute("href", "/forgot-password");
  });

  it('shows "Criar conta" link', () => {
    renderLoginPage();

    const createLink = screen.getByRole("link", { name: /criar conta/i });
    expect(createLink).toBeInTheDocument();
    expect(createLink).toHaveAttribute("href", "/register");
  });
});
