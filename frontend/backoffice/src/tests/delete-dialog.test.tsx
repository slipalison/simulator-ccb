import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DeleteDialog } from "@/components/molecules/DeleteDialog";
import * as adminApi from "@/lib/admin-api";

// Mock admin-api module
vi.mock("@/lib/admin-api", () => ({
  AdminApiError: class AdminApiError extends Error {
    public status?: number;
    constructor(message: string, status?: number) {
      super(message);
      this.name = "AdminApiError";
      this.status = status;
    }
  },
}));

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
    info: vi.fn(),
  },
}));

const defaultProps = {
  userName: "Joao Silva",
  userEmail: "joao@example.com",
  userDocument: "123.456.789-00",
  onDelete: vi.fn(),
  onSuccess: vi.fn(),
  onClose: vi.fn(),
  open: true,
};

describe("DeleteDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with title and warning message", () => {
    render(<DeleteDialog {...defaultProps} />);

    // Use getByRole for the dialog title (h2)
    expect(screen.getByRole("heading", { name: "Delete User" })).toBeInTheDocument();
    expect(screen.getByText(/PERMANENT/)).toBeInTheDocument();
    expect(screen.getByText(/LGPD/)).toBeInTheDocument();
  });

  it("displays user info (name, email, document)", () => {
    render(<DeleteDialog {...defaultProps} />);

    expect(screen.getByText("Joao Silva")).toBeInTheDocument();
    expect(screen.getByText("joao@example.com")).toBeInTheDocument();
    expect(screen.getByText("123.456.789-00")).toBeInTheDocument();
  });

  it("does not display document when not provided", () => {
    render(<DeleteDialog {...defaultProps} userDocument={undefined} />);

    expect(screen.getByText("Joao Silva")).toBeInTheDocument();
    expect(screen.getByText("joao@example.com")).toBeInTheDocument();
    // Document should not appear
    expect(screen.queryByText("123.456.789-00")).not.toBeInTheDocument();
  });

  it("confirm button disabled when email does not match", () => {
    render(<DeleteDialog {...defaultProps} />);

    const confirmButton = screen.getByTestId("confirm-delete-button");
    expect(confirmButton).toBeDisabled();

    // Type wrong email
    const input = screen.getByTestId("email-confirm-input");
    fireEvent.change(input, { target: { value: "wrong@email.com" } });

    expect(confirmButton).toBeDisabled();
    expect(screen.getByTestId("email-mismatch-error")).toBeInTheDocument();
  });

  it("confirm button enabled when email matches (case-insensitive)", async () => {
    render(<DeleteDialog {...defaultProps} />);

    const input = screen.getByTestId("email-confirm-input");
    const confirmButton = screen.getByTestId("confirm-delete-button");

    // Type exact email with different case
    await userEvent.type(input, "JOAO@EXAMPLE.COM");

    expect(confirmButton).not.toBeDisabled();
    expect(screen.queryByTestId("email-mismatch-error")).not.toBeInTheDocument();
  });

  it("calls onDelete with correct userId on confirm", async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined);
    render(<DeleteDialog {...defaultProps} onDelete={onDelete} />);

    const input = screen.getByTestId("email-confirm-input");
    await userEvent.type(input, "joao@example.com");

    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(onDelete).toHaveBeenCalled();
    });
  });

  it("shows loading state during submit", async () => {
    const onDelete = vi.fn().mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );
    render(<DeleteDialog {...defaultProps} onDelete={onDelete} />);

    const input = screen.getByTestId("email-confirm-input");
    await userEvent.type(input, "joao@example.com");

    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(screen.getByText(/Deleting/)).toBeInTheDocument();
    });
  });

  it("shows success toast and calls onSuccess on successful delete", async () => {
    const { toast } = await import("sonner");
    const onSuccess = vi.fn();
    const onDelete = vi.fn().mockResolvedValue(undefined);

    render(
      <DeleteDialog
        {...defaultProps}
        onDelete={onDelete}
        onSuccess={onSuccess}
      />
    );

    const input = screen.getByTestId("email-confirm-input");
    await userEvent.type(input, "joao@example.com");

    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith("Usuario deletado com sucesso.");
    });

    expect(onSuccess).toHaveBeenCalled();
    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it("shows error toast on 409 (already deleted)", async () => {
    const { toast } = await import("sonner");
    const onDelete = vi.fn().mockRejectedValue(
      new adminApi.AdminApiError("Usuario ja foi deletado.", 409)
    );

    render(<DeleteDialog {...defaultProps} onDelete={onDelete} />);

    const input = screen.getByTestId("email-confirm-input");
    await userEvent.type(input, "joao@example.com");

    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Usuario ja foi deletado.");
    });
  });

  it("shows error toast on other API failures", async () => {
    const { toast } = await import("sonner");
    const onDelete = vi.fn().mockRejectedValue(
      new Error("Network error")
    );

    render(<DeleteDialog {...defaultProps} onDelete={onDelete} />);

    const input = screen.getByTestId("email-confirm-input");
    await userEvent.type(input, "joao@example.com");

    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Falha ao deletar usuario", {
        description: "Tente novamente.",
      });
    });
  });

  it("onCancel called when cancel button clicked", async () => {
    render(<DeleteDialog {...defaultProps} />);

    fireEvent.click(screen.getByTestId("cancel-button"));

    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it("clears input when dialog is closed and reopened", () => {
    const { rerender } = render(<DeleteDialog {...defaultProps} open={false} />);

    // Dialog not visible when closed
    expect(screen.queryByText("Delete User")).not.toBeInTheDocument();

    // Reopen
    rerender(<DeleteDialog {...defaultProps} open={true} />);

    expect(screen.getByRole("heading", { name: "Delete User" })).toBeInTheDocument();
    const input = screen.getByTestId("email-confirm-input");
    expect(input).toHaveValue("");
  });
});
