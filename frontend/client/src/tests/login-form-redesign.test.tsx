import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { AuthLoginPage } from "@/components/pages/AuthLoginPage";
import { AuthCallbackPage } from "@/components/pages/AuthCallbackPage";
import { AuthErrorPage } from "@/components/pages/AuthErrorPage";
import { AuthProvider } from "@/lib/auth-context";

// Mock fetch for /auth/me
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock auth context
const mockLogin = vi.fn();
vi.mock("@/lib/auth-context", () => ({
  useAuth: () => ({
    login: mockLogin,
    auth: { isAuthenticated: false, isLoading: false, userName: null, email: null },
    logout: vi.fn(),
  }),
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}));

describe("AuthLoginPage — redirect-only component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  it("renders loading spinner", async () => {
    render(
      <AuthProvider>
        <AuthLoginPage />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByText(/redirecionando/i)).toBeInTheDocument();
    });
  });

  it("calls login() to redirect to /auth/login when not authenticated", async () => {
    render(
      <AuthProvider>
        <AuthLoginPage />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalled();
    });
  });
});

describe("AuthCallbackPage — session polling component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  it("renders loading spinner while polling", () => {
    mockFetch.mockImplementation(() => new Promise(() => {}));

    render(<AuthCallbackPage />);

    expect(screen.getByText(/concluindo/i)).toBeInTheDocument();
  });

  it("shows error card after max polling attempts fail", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 401 });

    render(<AuthCallbackPage />);

    await waitFor(
      () => {
        expect(screen.getByText(/falha ao estabelecer/i)).toBeInTheDocument();
      },
      { timeout: 5000 }
    );

    expect(screen.getByRole("link", { name: /voltar ao login/i })).toBeInTheDocument();
  });
});

describe("AuthErrorPage — error display component", () => {
  it("renders error title and link back to login", () => {
    Object.defineProperty(window, "location", {
      value: { search: "?error=access_denied" },
      writable: true,
    });

    render(<AuthErrorPage />);

    expect(screen.getByText(/erro de autenticação/i)).toBeInTheDocument();
    const link = screen.getByRole("link", { name: /tentar novamente/i });
    expect(link).toBeInTheDocument();
    expect(link).toHaveAttribute("href", "/auth/login");
  });

  it("displays decoded error message from URL param", () => {
    Object.defineProperty(window, "location", {
      value: { search: "?error=acesso%20negado" },
      writable: true,
    });

    render(<AuthErrorPage />);

    expect(screen.getByText("acesso negado")).toBeInTheDocument();
  });

  it("shows fallback message when no error param", () => {
    Object.defineProperty(window, "location", {
      value: { search: "" },
      writable: true,
    });

    render(<AuthErrorPage />);

    expect(screen.getByText(/ocorreu um erro/i)).toBeInTheDocument();
  });
});
