import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreateAdminPage } from "@/components/pages/CreateAdminPage";
import * as adminApi from "@/lib/admin-api";

vi.mock("@/lib/admin-api", () => ({
  createAdmin: vi.fn(),
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

vi.stubGlobal("navigator", { clipboard: { writeText: vi.fn() } });

describe("CreateAdminPage", () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it("renders form with name and email fields", () => {
    render(<CreateAdminPage />);
    expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /criar admin/i })).toBeInTheDocument();
  });

  it("shows validation errors for empty fields on submit", async () => {
    render(<CreateAdminPage />);
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/nome deve ter pelo menos 2 caracteres/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/email invalido/i)).toBeInTheDocument();
  });

  it("calls createAdmin with fullName and email on valid submit", async () => {
    vi.mocked(adminApi.createAdmin).mockResolvedValue({ adminId: "a1", temporaryPassword: "T@1234!abc" });
    render(<CreateAdminPage />);
    await act(async () => {
      await userEvent.type(screen.getByLabelText(/nome completo/i), "Joao Silva");
      await userEvent.type(screen.getByLabelText(/email/i), "joao@test.com");
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(adminApi.createAdmin).toHaveBeenCalledWith("Joao Silva", "joao@test.com");
    });
  });

  it("shows temporary password result card after successful creation", async () => {
    vi.mocked(adminApi.createAdmin).mockResolvedValue({ adminId: "a1", temporaryPassword: "T@1234!abc" });
    render(<CreateAdminPage />);
    await act(async () => {
      await userEvent.type(screen.getByLabelText(/nome completo/i), "Joao Silva");
      await userEvent.type(screen.getByLabelText(/email/i), "joao@test.com");
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/admin criado com sucesso/i)).toBeInTheDocument();
    });
    expect(screen.getAllByText(/senha temporaria/i).length).toBeGreaterThanOrEqual(1);
  });

  it("copy password button copies to clipboard", async () => {
    vi.mocked(adminApi.createAdmin).mockResolvedValue({ adminId: "a1", temporaryPassword: "T@1234!abc" });
    render(<CreateAdminPage />);
    await act(async () => {
      await userEvent.type(screen.getByLabelText(/nome completo/i), "Joao Silva");
      await userEvent.type(screen.getByLabelText(/email/i), "joao@test.com");
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/admin criado com sucesso/i)).toBeInTheDocument();
    });
    await act(async () => {
      fireEvent.click(screen.getByTitle(/copiar senha/i));
    });
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith("T@1234!abc");
  });

  it("Create Another button clears result and resets form", async () => {
    vi.mocked(adminApi.createAdmin).mockResolvedValue({ adminId: "a1", temporaryPassword: "T@1234!abc" });
    render(<CreateAdminPage />);
    await act(async () => {
      await userEvent.type(screen.getByLabelText(/nome completo/i), "Joao Silva");
      await userEvent.type(screen.getByLabelText(/email/i), "joao@test.com");
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(screen.getByText(/admin criado com sucesso/i)).toBeInTheDocument();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /criar outro admin/i }));
    });
    await waitFor(() => {
      expect(screen.queryByText(/admin criado com sucesso/i)).not.toBeInTheDocument();
    });
  });

  it("shows error toast on 409 conflict", async () => {
    const { toast } = await import("sonner");
    vi.mocked(adminApi.createAdmin).mockRejectedValue({ status: 409 });
    render(<CreateAdminPage />);
    await act(async () => {
      await userEvent.type(screen.getByLabelText(/nome completo/i), "Joao Silva");
      await userEvent.type(screen.getByLabelText(/email/i), "joao@test.com");
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Email ja cadastrado", expect.any(Object));
    });
  });

  it("shows error toast on other failures", async () => {
    const { toast } = await import("sonner");
    vi.mocked(adminApi.createAdmin).mockRejectedValue({ status: 500 });
    render(<CreateAdminPage />);
    await act(async () => {
      await userEvent.type(screen.getByLabelText(/nome completo/i), "Joao Silva");
      await userEvent.type(screen.getByLabelText(/email/i), "joao@test.com");
      fireEvent.click(screen.getByRole("button", { name: /criar admin/i }));
    });
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Falha ao criar admin", expect.any(Object));
    });
  });
});
