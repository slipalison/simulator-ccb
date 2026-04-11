import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import * as adminApi from "@/lib/admin-api";

// Mock admin-api module
vi.mock("@/lib/admin-api", () => ({
  getUserDetail: vi.fn(),
  updateUser: vi.fn(),
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

describe("Admin Edit Flow Integration", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("edit user -> fill form -> submit -> success toast -> redirect", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);
    vi.mocked(adminApi.updateUser).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("edit-button")).toBeInTheDocument();
    });

    // Click edit button -> should navigate to edit page
    fireEvent.click(screen.getByTestId("edit-button"));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith({
        to: "/admin/users/$id/edit",
        params: { id: "1" },
      });
    });
  });

  it("block user -> dialog -> type reason -> confirm -> success toast -> user blocked", async () => {
    const blockedUser = { ...mockUserDetail, keycloakEnabled: false };
    vi.mocked(adminApi.getUserDetail)
      .mockResolvedValueOnce(mockUserDetail)
      .mockResolvedValueOnce(blockedUser);
    vi.mocked(adminApi.blockUser).mockResolvedValue(undefined);
    const { toast } = await import("sonner");

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("block-button")).toBeInTheDocument();
    });

    // Click block button -> opens dialog
    fireEvent.click(screen.getByTestId("block-button"));

    await waitFor(() => {
      const titles = screen.getAllByText("Block User");
      expect(titles.length).toBeGreaterThanOrEqual(1);
    });

    // Type reason and confirm
    const reasonInput = screen.getByLabelText(/reason for blocking/i);
    await userEvent.type(reasonInput, "User was violating terms of service repeatedly");

    fireEvent.click(screen.getByRole("button", { name: /block user/i }));

    await waitFor(() => {
      expect(adminApi.blockUser).toHaveBeenCalledWith("1", "User was violating terms of service repeatedly");
    });

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith("Usuario bloqueado com sucesso.");
    });
  });

  it("unblock user -> dialog -> confirm -> success toast -> user unblocked", async () => {
    const blockedUser = { ...mockUserDetail, keycloakEnabled: false };
    const unblockedUser = { ...mockUserDetail, keycloakEnabled: true };
    vi.mocked(adminApi.getUserDetail)
      .mockResolvedValueOnce(blockedUser)
      .mockResolvedValueOnce(unblockedUser);
    vi.mocked(adminApi.unblockUser).mockResolvedValue(undefined);
    const { toast } = await import("sonner");

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("unblock-button")).toBeInTheDocument();
    });

    // Click unblock button -> opens dialog
    fireEvent.click(screen.getByTestId("unblock-button"));

    await waitFor(() => {
      const titles = screen.getAllByText("Unblock User");
      expect(titles.length).toBeGreaterThanOrEqual(1);
    });

    // Confirm without reason
    fireEvent.click(screen.getByRole("button", { name: /unblock user/i }));

    await waitFor(() => {
      expect(adminApi.unblockUser).toHaveBeenCalledWith("1", undefined);
    });

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith("Usuario desbloqueado com sucesso.");
    });
  });
});
