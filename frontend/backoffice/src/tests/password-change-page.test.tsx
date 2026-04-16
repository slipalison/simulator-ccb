import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { PasswordChangePage } from "@/components/pages/PasswordChangePage";
import * as adminApi from "@/lib/admin-api";

vi.mock("@/lib/admin-api", () => ({
  forcePasswordChange: vi.fn(),
  AdminApiError: class AdminApiError extends Error {
    public status?: number;
    constructor(message: string, status?: number) {
      super(message);
      this.name = "AdminApiError";
      this.status = status;
    }
  },
}));

vi.mock("sonner", () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}));

describe("PasswordChangePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders password change form with new password and confirm fields", () => {
    render(<PasswordChangePage />);
    // Password inputs are type="password" (not textbox), so use placeholder
    expect(screen.getByPlaceholderText(/digite sua nova senha/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/confirme sua nova senha/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /alterar senha/i })).toBeInTheDocument();
  });

  it("shows validation error for password too short", async () => {
    render(<PasswordChangePage />);
    const inputs = screen.getAllByLabelText(/nova senha/i);
    const confirmInputs = screen.getAllByLabelText(/confirmar nova senha/i);
    await act(async () => {
      await userEvent.type(inputs[0], "abc");
      await userEvent.type(confirmInputs[0], "abc");
      fireEvent.click(screen.getByRole("button", { name: /alterar senha/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/senha deve ter pelo menos 8 caracteres/i)).toBeInTheDocument();
    });
  });

  it("shows validation error for missing uppercase", async () => {
    render(<PasswordChangePage />);
    const inputs = screen.getAllByLabelText(/nova senha/i);
    const confirmInputs = screen.getAllByLabelText(/confirmar nova senha/i);
    await act(async () => {
      await userEvent.type(inputs[0], "abcdefgh1!");
      await userEvent.type(confirmInputs[0], "abcdefgh1!");
      fireEvent.click(screen.getByRole("button", { name: /alterar senha/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/deve conter pelo menos uma letra maiuscula/i)).toBeInTheDocument();
    });
  });

  it("shows validation error for passwords not matching", async () => {
    render(<PasswordChangePage />);
    const inputs = screen.getAllByLabelText(/nova senha/i);
    const confirmInputs = screen.getAllByLabelText(/confirmar nova senha/i);
    await act(async () => {
      await userEvent.type(inputs[0], "Secure@123");
      await userEvent.type(confirmInputs[0], "Different@123");
      fireEvent.click(screen.getByRole("button", { name: /alterar senha/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/as senhas nao coincidem/i)).toBeInTheDocument();
    });
  });

  it("calls forcePasswordChange with new password on valid submit", async () => {
    vi.mocked(adminApi.forcePasswordChange).mockResolvedValue(undefined);
    render(<PasswordChangePage />);
    const inputs = screen.getAllByLabelText(/nova senha/i);
    const confirmInputs = screen.getAllByLabelText(/confirmar nova senha/i);
    await act(async () => {
      await userEvent.type(inputs[0], "Secure@123");
      await userEvent.type(confirmInputs[0], "Secure@123");
      fireEvent.click(screen.getByRole("button", { name: /alterar senha/i }));
    });
    await waitFor(() => {
      expect(adminApi.forcePasswordChange).toHaveBeenCalledWith("Secure@123");
    });
  });

  it("shows success card after password change", async () => {
    vi.mocked(adminApi.forcePasswordChange).mockResolvedValue(undefined);
    render(<PasswordChangePage />);
    const inputs = screen.getAllByLabelText(/nova senha/i);
    const confirmInputs = screen.getAllByLabelText(/confirmar nova senha/i);
    await act(async () => {
      await userEvent.type(inputs[0], "Secure@123");
      await userEvent.type(confirmInputs[0], "Secure@123");
      fireEvent.click(screen.getByRole("button", { name: /alterar senha/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/senha alterada!/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/acessar painel/i)).toBeInTheDocument();
  });

  it("shows error toast on failure", async () => {
    const { toast } = await import("sonner");
    vi.mocked(adminApi.forcePasswordChange).mockRejectedValue(new Error("Failed"));
    render(<PasswordChangePage />);
    const inputs = screen.getAllByLabelText(/nova senha/i);
    const confirmInputs = screen.getAllByLabelText(/confirmar nova senha/i);
    await act(async () => {
      await userEvent.type(inputs[0], "Secure@123");
      await userEvent.type(confirmInputs[0], "Secure@123");
      fireEvent.click(screen.getByRole("button", { name: /alterar senha/i }));
    });
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Falha ao alterar senha", expect.any(Object));
    });
  });
});
