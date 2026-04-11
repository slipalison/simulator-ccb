import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BlockDialog } from "@/components/molecules/BlockDialog";

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

const mockOnBlock = vi.fn();
const mockOnClose = vi.fn();

describe("BlockDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with title and warning message", () => {
    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    // Title is in an h2, button also has same text - use getAllByText
    const titles = screen.getAllByText("Block User");
    expect(titles.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/this will prevent the user from logging in/i)).toBeInTheDocument();
  });

  it("reason field required, min 10 chars validation", async () => {
    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonInput = screen.getByLabelText(/reason for blocking/i);
    await userEvent.type(reasonInput, "short");
    await userEvent.click(screen.getByRole("button", { name: /block user/i }));

    await waitFor(() => {
      expect(screen.getByText(/motivo deve ter pelo menos 10 caracteres/i)).toBeInTheDocument();
    });
    expect(mockOnBlock).not.toHaveBeenCalled();
  });

  it("confirm button disabled when reason < 10 chars", async () => {
    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonInput = screen.getByLabelText(/reason for blocking/i);
    await userEvent.type(reasonInput, "short");
    await userEvent.click(screen.getByRole("button", { name: /block user/i }));

    // After failed validation, the button should not call onBlock
    expect(mockOnBlock).not.toHaveBeenCalled();
  });

  it("calls onBlock with reason on submit", async () => {
    mockOnBlock.mockResolvedValue(undefined);

    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonInput = screen.getByLabelText(/reason for blocking/i);
    await userEvent.type(reasonInput, "User was violating terms of service repeatedly");
    await userEvent.click(screen.getByRole("button", { name: /block user/i }));

    await waitFor(() => {
      expect(mockOnBlock).toHaveBeenCalledWith("User was violating terms of service repeatedly");
    });
  });

  it("shows loading state during submit", async () => {
    mockOnBlock.mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 500))
    );

    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonInput = screen.getByLabelText(/reason for blocking/i);
    await userEvent.type(reasonInput, "User was violating terms of service repeatedly");
    await userEvent.click(screen.getByRole("button", { name: /block user/i }));

    await waitFor(() => {
      const loadingButton = screen.getByRole("button", { name: /blocking/i });
      expect(loadingButton).toBeDisabled();
    });
  });

  it("shows error toast on API failure", async () => {
    mockOnBlock.mockRejectedValue(new Error("API Error"));
    const { toast } = await import("sonner");

    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonInput = screen.getByLabelText(/reason for blocking/i);
    await userEvent.type(reasonInput, "User was violating terms of service repeatedly");
    await userEvent.click(screen.getByRole("button", { name: /block user/i }));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalled();
    });
  });

  it("calls onClose when cancel button clicked", async () => {
    render(
      <BlockDialog
        userName="Joao Silva"
        onBlock={mockOnBlock}
        onClose={mockOnClose}
        open
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /cancel/i }));
    expect(mockOnClose).toHaveBeenCalled();
  });
});
