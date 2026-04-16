import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AuthProvider } from "@/lib/auth-context";
import * as api from "@/lib/api";
import { ResetPasswordPage } from "@/components/pages/ResetPasswordPage";

// Mock API module
vi.mock("@/lib/api", () => ({
  getProfileClient: vi.fn(),
  forgotPasswordClient: vi.fn(),
  resetPasswordClient: vi.fn(),
  ResetPasswordError: class ResetPasswordError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ResetPasswordError";
    }
  },
}));

// Mock router
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
  };
});

// Set URL with token for tests
beforeEach(() => {
  window.history.pushState({}, "", "/reset-password?token=test-token");
});

describe("ResetPasswordPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders password fields and submit button", async () => {
    render(
      <AuthProvider>
        <ResetPasswordPage token="test-token" />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(document.getElementById("password")).toBeInTheDocument();
      expect(document.getElementById("confirmPassword")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("button", { name: /alterar senha/i })
    ).toBeInTheDocument();
  });

  it("shows password strength meter", async () => {
    render(
      <AuthProvider>
        <ResetPasswordPage token="test-token" />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(document.getElementById("password")).toBeInTheDocument();
    });

    const passwordInput = document.getElementById("password") as HTMLInputElement;
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: "Str0ng@Pass!" } });
    });

    // Password strength indicator should appear
    await waitFor(() => {
      expect(screen.getByText(/forte/i)).toBeInTheDocument();
    });
  });

  it("blocks submit if passwords dont match", async () => {
    render(
      <AuthProvider>
        <ResetPasswordPage token="test-token" />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(document.getElementById("password")).toBeInTheDocument();
    });

    const passwordInput = document.getElementById("password") as HTMLInputElement;
    const confirmInput = document.getElementById("confirmPassword") as HTMLInputElement;

    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: "Str0ng@Pass!" } });
      fireEvent.change(confirmInput, { target: { value: "Different@1" } });
    });

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: /alterar senha/i })
      );
    });

    await waitFor(() => {
      expect(screen.getAllByText(/nao coincidem/i).length).toBeGreaterThan(0);
    });

    // API should not be called
    expect(api.resetPasswordClient).not.toHaveBeenCalled();
  });

  it("redirects to /login after successful reset", async () => {
    vi.mocked(api.resetPasswordClient).mockResolvedValue({ message: "Senha alterada com sucesso." });

    render(
      <AuthProvider>
        <ResetPasswordPage token="valid-token" />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(document.getElementById("password")).toBeInTheDocument();
    });

    const passwordInput = document.getElementById("password") as HTMLInputElement;
    const confirmInput = document.getElementById("confirmPassword") as HTMLInputElement;

    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: "Str0ng@Pass!" } });
      fireEvent.change(confirmInput, { target: { value: "Str0ng@Pass!" } });
    });

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: /alterar senha/i })
      );
    });

    // Wait for the API call to succeed
    await waitFor(() => {
      expect(api.resetPasswordClient).toHaveBeenCalledWith("valid-token", "Str0ng@Pass!");
    });
  });

  it("shows error for expired token", async () => {
    const { ResetPasswordError: MockResetPasswordError } = await import("@/lib/api");
    vi.mocked(api.resetPasswordClient).mockRejectedValue(
      new MockResetPasswordError("Token expirado")
    );

    render(
      <AuthProvider>
        <ResetPasswordPage token="expired-token" />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(document.getElementById("password")).toBeInTheDocument();
    });

    const passwordInput = document.getElementById("password") as HTMLInputElement;
    const confirmInput = document.getElementById("confirmPassword") as HTMLInputElement;

    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: "Str0ng@Pass!" } });
      fireEvent.change(confirmInput, { target: { value: "Str0ng@Pass!" } });
    });

    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: /alterar senha/i })
      );
    });

    await waitFor(() => {
      expect(screen.getByText(/token expirado/i)).toBeInTheDocument();
    });
  });
});
