import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ResetPasswordPage } from "@/components/pages/ResetPasswordPage";
import * as api from "@/lib/api";

// Mock API
vi.mock("@/lib/api", () => ({
  resetPasswordClient: vi.fn(),
  ResetPasswordError: class extends Error {
    constructor(message: string) { super(message); this.name = "ResetPasswordError"; }
  },
  ApiError: class extends Error {
    constructor(message: string) { super(message); this.name = "ApiError"; }
  },
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

// Set URL with token for tests
beforeEach(() => {
  window.history.pushState({}, "", "/reset-password?token=test-reset-token");
});

function renderResetPasswordPage(token?: string) {
  return render(<ResetPasswordPage token={token} />);
}

describe("ResetPasswordPage (shadcn redesign)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with shadcn Card container", () => {
    renderResetPasswordPage("test-token");

    expect(screen.getByText("Nova Senha")).toBeInTheDocument();
  });

  it("shows password field with show/hide toggle", () => {
    renderResetPasswordPage("test-token");

    // Use ID to get specific password input
    const passwordInput = document.getElementById("password") as HTMLInputElement;
    expect(passwordInput).toBeInTheDocument();

    // Toggle button should exist
    const toggleButtons = screen.getAllByRole("button", { name: /mostrar senha|ocultar senha/i });
    expect(toggleButtons.length).toBeGreaterThan(0);
  });

  it("shows password strength meter", async () => {
    const user = userEvent.setup();
    renderResetPasswordPage("test-token");

    const passwordInput = document.getElementById("password") as HTMLInputElement;
    await user.type(passwordInput, "Str0ng!Pass");

    await waitFor(() => {
      expect(screen.getByText(/media|fraca|forte|muito/i)).toBeInTheDocument();
    });
  });

  it("shows confirm password field", () => {
    renderResetPasswordPage("test-token");

    const confirmInput = document.getElementById("confirmPassword") as HTMLInputElement;
    expect(confirmInput).toBeInTheDocument();
  });

  it("shows submit button", () => {
    renderResetPasswordPage("test-token");

    const submitButton = screen.getByRole("button", { name: /alterar senha/i });
    expect(submitButton).toBeInTheDocument();
  });

  it("redirects to login after successful reset", async () => {
    const user = userEvent.setup();
    vi.mocked(api.resetPasswordClient).mockResolvedValue({ message: "Senha alterada" });

    renderResetPasswordPage("test-token");

    const passwordInput = document.getElementById("password") as HTMLInputElement;
    const confirmPasswordInput = document.getElementById("confirmPassword") as HTMLInputElement;

    await user.type(passwordInput, "Str0ng!Pass");
    await user.type(confirmPasswordInput, "Str0ng!Pass");

    await user.click(screen.getByRole("button", { name: /alterar senha/i }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(expect.objectContaining({ to: "/login" }));
    });
  });
});
