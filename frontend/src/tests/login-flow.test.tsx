import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AuthProvider, useAuth } from "@/lib/auth-context";
import { LoginPage } from "@/components/pages/LoginPage";
import { ProfilePage } from "@/components/pages/ProfilePage";
import * as api from "@/lib/api";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { router } from "@/router";

// Mock the entire API module
vi.mock("@/lib/api", () => ({
  loginClient: vi.fn(),
  refreshTokenClient: vi.fn(),
  getProfileClient: vi.fn().mockResolvedValue({
    id: "test-id",
    name: "Test User",
    email: "test@example.com",
    phone: "(11) 99999-9999",
    type: "PessoaFisica",
    cpf: "123.456.789-00",
    cnpj: null,
    razaoSocial: null,
  }),
  LoginError: class LoginError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "LoginError";
    }
  },
  ApiError: class ApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ApiError";
    }
  },
  ProfileError: class ProfileError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ProfileError";
    }
  },
}));

function wrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

async function renderWithRouter(initialEntries: string[] = ["/login"]) {
  const memoryHistory = createMemoryHistory({ initialEntries });
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
  return { ...view, router: testRouter, memoryHistory };
}

describe("AUTH-01: Login flow — form to redirect", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("LoginPage renders LoginForm with email and password fields", async () => {
    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/senha/i)).toBeInTheDocument();
    });

    expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
  });

  it("Submitting empty form shows validation errors", async () => {
    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    // Submit without filling fields
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    await waitFor(() => {
      // Zod validation errors should appear
      const alerts = screen.getAllByRole("alert");
      expect(alerts.length).toBeGreaterThan(0);
    });

    // API should not be called for invalid form
    expect(api.loginClient).not.toHaveBeenCalled();
  });

  it("Successful login redirects to /profile", async () => {
    const mockResponse = {
      accessToken: "mock-access-token",
      refreshToken: "mock-refresh-token",
      expiresIn: 300,
      tokenType: "Bearer",
      refreshExpiresIn: 86400,
      scope: "openid",
    };
    vi.mocked(api.loginClient).mockResolvedValue(mockResponse);

    const { memoryHistory } = await renderWithRouter(["/login"]);

    // Wait for form to render
    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    // Fill form
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "test@example.com" } });
      fireEvent.change(passwordInput, { target: { value: "password123" } });
    });

    // Submit
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Wait for redirect
    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });
  });

  it("Failed login shows error message", async () => {
    vi.mocked(api.loginClient).mockRejectedValue(
      new api.LoginError("Invalid credentials.")
    );

    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    // Wait for form to render
    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    // Fill form
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "wrong@example.com" } });
      fireEvent.change(passwordInput, { target: { value: "wrongpassword" } });
    });

    // Submit
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Wait for error message
    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });
  });

  it("Form is not cleared on login failure — email field retains value", async () => {
    vi.mocked(api.loginClient).mockRejectedValue(
      new api.LoginError("Invalid credentials.")
    );

    render(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );

    // Wait for form to render
    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    // Fill form
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "retain@example.com" } });
      fireEvent.change(passwordInput, { target: { value: "password123" } });
    });

    // Submit
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Wait for error and verify email is retained
    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });

    expect((emailInput as HTMLInputElement).value).toBe("retain@example.com");
  });
});

describe("Profile guard — unauthenticated redirect", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("ProfilePage redirects to /login when unauthenticated", async () => {
    const { memoryHistory } = await renderWithRouter(["/profile"]);

    // Wait for redirect to /login
    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/login");
    });
  });

  it("ProfilePage shows placeholder when authenticated", async () => {
    // First, login to set auth state
    const mockResponse = {
      accessToken: "mock-access-token",
      refreshToken: "mock-refresh-token",
      expiresIn: 300,
      tokenType: "Bearer",
      refreshExpiresIn: 86400,
      scope: "openid",
    };
    vi.mocked(api.loginClient).mockResolvedValue(mockResponse);

    // Start at login, fill form, submit to get authenticated
    const { memoryHistory } = await renderWithRouter(["/login"]);

    await waitFor(() => {
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    });

    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "test@example.com" } });
      fireEvent.change(passwordInput, { target: { value: "password123" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Wait for redirect to profile
    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/profile");
    });

    // Now profile page should show the profile heading
    await waitFor(() => {
      expect(screen.getByText(/Meu Perfil/i)).toBeInTheDocument();
    });

    // And logout (Sair) button should be visible
    expect(screen.getByRole("button", { name: /sair/i })).toBeInTheDocument();
  });
});
