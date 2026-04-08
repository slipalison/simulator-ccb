import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { ForgotPasswordPage } from "@/components/pages/ForgotPasswordPage";
import * as api from "@/lib/api";

// Mock API
vi.mock("@/lib/api", () => ({
  forgotPasswordClient: vi.fn(),
  ForgotPasswordError: class extends Error {
    constructor(message: string) { super(message); this.name = "ForgotPasswordError"; }
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

function renderForgotPasswordPage() {
  return render(<ForgotPasswordPage />);
}

describe("ForgotPasswordPage (shadcn redesign)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with shadcn Card container", () => {
    renderForgotPasswordPage();

    expect(screen.getByText("Recuperar Senha")).toBeInTheDocument();
  });

  it("shows email input with shadcn Input", () => {
    renderForgotPasswordPage();

    const emailInput = screen.getByLabelText(/email/i);
    expect(emailInput).toBeInTheDocument();
    expect(emailInput).toHaveAttribute("placeholder", "seu@email.com");
  });

  it("shows submit button with shadcn Button", () => {
    renderForgotPasswordPage();

    const submitButton = screen.getByRole("button", { name: /enviar link/i });
    expect(submitButton).toBeInTheDocument();
  });

  it("shows success message after submitting", async () => {
    vi.mocked(api.forgotPasswordClient).mockResolvedValue({ message: "Email enviado" });

    renderForgotPasswordPage();

    const emailInput = screen.getByLabelText(/email/i);
    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "test@example.com" } });
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /enviar link/i }));
    });

    await waitFor(() => {
      expect(screen.getByText(/Email Enviado/i)).toBeInTheDocument();
    });
  });

  it("shows link back to login", () => {
    renderForgotPasswordPage();

    const loginLink = screen.getByRole("link", { name: /voltar para login/i });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/login");
  });
});
