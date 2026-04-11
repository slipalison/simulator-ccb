import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { UnblockDialog } from "@/components/molecules/UnblockDialog";

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

const mockOnUnblock = vi.fn();
const mockOnClose = vi.fn();

describe("UnblockDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with title and info message", () => {
    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    const titles = screen.getAllByText("Unblock User");
    expect(titles.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/this will restore user access/i)).toBeInTheDocument();
  });

  it("reason field optional", () => {
    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonLabel = screen.getByText(/reason for unblocking/i);
    expect(reasonLabel).toBeInTheDocument();
    expect(reasonLabel).toHaveTextContent("optional");
  });

  it("confirm button enabled without reason", () => {
    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    const confirmButton = screen.getByRole("button", { name: /unblock user/i });
    expect(confirmButton).not.toBeDisabled();
  });

  it("calls onUnblock without reason on submit", async () => {
    mockOnUnblock.mockResolvedValue(undefined);

    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /unblock user/i }));

    await waitFor(() => {
      expect(mockOnUnblock).toHaveBeenCalledWith(undefined);
    });
  });

  it("calls onUnblock with optional reason on submit", async () => {
    mockOnUnblock.mockResolvedValue(undefined);

    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    const reasonInput = screen.getByLabelText(/reason for unblocking/i);
    await userEvent.type(reasonInput, "User appealed and was reinstated");
    await userEvent.click(screen.getByRole("button", { name: /unblock user/i }));

    await waitFor(() => {
      expect(mockOnUnblock).toHaveBeenCalledWith("User appealed and was reinstated");
    });
  });

  it("shows loading state during submit", async () => {
    mockOnUnblock.mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 500))
    );

    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /unblock user/i }));

    await waitFor(() => {
      const loadingButton = screen.getByRole("button", { name: /unblocking/i });
      expect(loadingButton).toBeDisabled();
    });
  });

  it("calls onClose when cancel button clicked", async () => {
    render(
      <UnblockDialog
        userName="Joao Silva"
        onUnblock={mockOnUnblock}
        onClose={mockOnClose}
        open
      />
    );

    await userEvent.click(screen.getByRole("button", { name: /cancel/i }));
    expect(mockOnClose).toHaveBeenCalled();
  });
});
