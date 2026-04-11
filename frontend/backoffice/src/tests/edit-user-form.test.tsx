import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { EditUserForm } from "@/components/molecules/EditUserForm";
import type { UserDetailDto } from "@/lib/admin-api";

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

const mockUser: UserDetailDto = {
  id: "1",
  name: "Joao Silva",
  email: "joao@example.com",
  phone: "(11) 99999-9999",
  document: "123.456.789-00",
  type: "PF",
  createdAt: "2026-01-15T10:00:00Z",
  keycloakEnabled: true,
  keycloakEmailVerified: true,
  keycloakUserId: "kc-123",
};

const mockOnUpdate = vi.fn();
const mockOnCancel = vi.fn();
const mockOnSuccess = vi.fn();

describe("EditUserForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with user data populated", () => {
    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    expect(screen.getByLabelText(/nome/i)).toHaveValue("Joao Silva");
    expect(screen.getByLabelText(/email/i)).toHaveValue("joao@example.com");
    expect(screen.getByLabelText(/telefone/i)).toHaveValue("(11) 99999-9999");
  });

  it("shows person type as readonly badge", () => {
    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    expect(screen.getByText("Pessoa Fisica")).toBeInTheDocument();
    expect(screen.getByText("123.456.789-00")).toBeInTheDocument();
  });

  it("shows PJ type for PJ user", () => {
    const pjUser = { ...mockUser, type: "PJ" as const, document: "12.345.678/0001-99", razaoSocial: "Acme Ltda" };
    render(
      <EditUserForm
        user={pjUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    expect(screen.getByText("Pessoa Juridica")).toBeInTheDocument();
  });

  it("shows validation errors for short name", async () => {
    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    const nameInput = screen.getByLabelText(/nome/i);
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "A");
    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByText(/nome deve ter pelo menos 2 caracteres/i)).toBeInTheDocument();
    });
    expect(mockOnUpdate).not.toHaveBeenCalled();
  });

  it("shows validation errors for invalid email", async () => {
    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    const emailInput = screen.getByLabelText(/email/i);
    await userEvent.clear(emailInput);
    await userEvent.type(emailInput, "not-an-email");
    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByText(/email invalido/i)).toBeInTheDocument();
    });
    expect(mockOnUpdate).not.toHaveBeenCalled();
  });

  it("shows validation errors for invalid phone format", async () => {
    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    const phoneInput = screen.getByLabelText(/telefone/i);
    await userEvent.clear(phoneInput);
    await userEvent.type(phoneInput, "123");
    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByText(/telefone deve estar no formato/i)).toBeInTheDocument();
    });
    expect(mockOnUpdate).not.toHaveBeenCalled();
  });

  it("calls onUpdate with correct data on valid submit", async () => {
    mockOnUpdate.mockResolvedValue(mockUser);

    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    const nameInput = screen.getByLabelText(/nome/i);
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "Joao Silva Updated");

    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(mockOnUpdate).toHaveBeenCalledWith({
        name: "Joao Silva Updated",
        email: "joao@example.com",
        phone: "(11) 99999-9999",
      });
    });
  });

  it("shows loading state during submit", async () => {
    mockOnUpdate.mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 500))
    );

    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    // Check for loading state - button should be disabled or show spinner
    await waitFor(() => {
      const saveButton = screen.getByRole("button", { name: /saving/i });
      expect(saveButton).toBeDisabled();
    });
  });

  it("shows error toast on API failure", async () => {
    mockOnUpdate.mockRejectedValue(new Error("API Error"));
    const { toast } = await import("sonner");

    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalled();
    });
  });

  it("calls onSuccess after successful submit", async () => {
    mockOnUpdate.mockResolvedValue(mockUser);

    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(mockOnSuccess).toHaveBeenCalled();
    });
  });

  it("calls onCancel when cancel button clicked", async () => {
    render(
      <EditUserForm
        user={mockUser}
        onUpdate={mockOnUpdate}
        onCancel={mockOnCancel}
        onSuccess={mockOnSuccess}
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /cancel/i }));
    expect(mockOnCancel).toHaveBeenCalled();
  });
});
