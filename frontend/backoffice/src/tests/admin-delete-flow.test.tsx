import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import { AdminUsersPage } from "@/components/pages/AdminUsersPage";
import * as adminApi from "@/lib/admin-api";

// Mock admin-api module
vi.mock("@/lib/admin-api", () => ({
  getUserDetail: vi.fn(),
  deleteUser: vi.fn(),
  listUsers: vi.fn(),
  blockUser: vi.fn(),
  unblockUser: vi.fn(),
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

// Mock TanStack Router
const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockUserDetail = {
  id: "1",
  name: "Joao Silva",
  email: "joao@example.com",
  phone: "(11) 99999-9999",
  document: "123.456.789-00",
  type: "PF" as const,
  createdAt: "2026-01-15T10:00:00Z",
  keycloakEnabled: true,
  keycloakEmailVerified: true,
  keycloakUserId: "kc-123",
};

const mockUsersResult = {
  items: [
    { id: "1", name: "Joao Silva", email: "joao@example.com", document: "123.456.789-00", type: "PF" as const, enabled: true },
    { id: "2", name: "Maria Santos", email: "maria@example.com", document: "987.654.321-00", type: "PF" as const, enabled: false },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

describe("Admin Delete Flow Integration", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("Delete user from detail page -> dialog -> type email -> confirm -> success toast -> redirect to list", async () => {
    const { toast } = await import("sonner");
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);
    vi.mocked(adminApi.deleteUser).mockResolvedValue(undefined);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("delete-button")).toBeInTheDocument();
    });

    // Click delete button -> opens dialog
    fireEvent.click(screen.getByTestId("delete-button"));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Delete User" })).toBeInTheDocument();
    });

    // Type exact email to confirm
    const emailInput = screen.getByTestId("email-confirm-input");
    await userEvent.type(emailInput, "joao@example.com");

    // Click confirm delete
    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(adminApi.deleteUser).toHaveBeenCalledWith("1");
    });

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith("Usuario deletado com sucesso.");
    });

    // Should navigate back to list
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/users" });
    });
  });

  it("Attempt to delete already-deleted user -> 409 error toast", async () => {
    const { toast } = await import("sonner");
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);
    vi.mocked(adminApi.deleteUser).mockRejectedValue(
      new adminApi.AdminApiError("Usuario ja foi deletado.", 409)
    );

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("delete-button")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("delete-button"));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Delete User" })).toBeInTheDocument();
    });

    const emailInput = screen.getByTestId("email-confirm-input");
    await userEvent.type(emailInput, "joao@example.com");

    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Usuario ja foi deletado.");
    });
  });

  it("Delete user from table -> dialog -> type email -> confirm -> success toast -> table refreshed", async () => {
    const { toast } = await import("sonner");
    vi.mocked(adminApi.listUsers)
      .mockResolvedValueOnce(mockUsersResult)
      .mockResolvedValueOnce({
        ...mockUsersResult,
        items: [
          { id: "2", name: "Maria Santos", email: "maria@example.com", document: "987.654.321-00", type: "PF" as const, enabled: false },
        ],
        totalCount: 1,
      });

    render(<AdminUsersPage />);

    // Wait for table to render
    await waitFor(() => {
      expect(screen.getByTestId("users-table")).toBeInTheDocument();
    });

    // Click delete button for user 1
    fireEvent.click(screen.getByTestId("delete-action-1"));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Delete User" })).toBeInTheDocument();
    });

    // Type exact email
    const emailInput = screen.getByTestId("email-confirm-input");
    await userEvent.type(emailInput, "joao@example.com");

    // Confirm delete
    fireEvent.click(screen.getByTestId("confirm-delete-button"));

    await waitFor(() => {
      expect(adminApi.deleteUser).toHaveBeenCalledWith("1");
    });

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith("Usuario deletado com sucesso.");
    });
  });

  it("Cancel delete closes dialog without calling API", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("delete-button")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("delete-button"));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Delete User" })).toBeInTheDocument();
    });

    // Click cancel
    fireEvent.click(screen.getByTestId("cancel-button"));

    await waitFor(() => {
      expect(screen.queryByRole("heading", { name: "Delete User" })).not.toBeInTheDocument();
    });

    expect(adminApi.deleteUser).not.toHaveBeenCalled();
  });

  it("Delete button disabled for deleted user in detail page", async () => {
    const deletedUser = { ...mockUserDetail, deletedAt: "2026-01-01T00:00:00Z" };
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(deletedUser);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("user-detail-card")).toBeInTheDocument();
    });

    // Delete button should not be in document for deleted users
    expect(screen.queryByTestId("delete-button")).not.toBeInTheDocument();
  });
});
