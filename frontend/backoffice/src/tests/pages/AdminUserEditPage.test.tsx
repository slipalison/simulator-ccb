// ---------------------------------------------------------------------------
// AdminUserEditPage — render, loading, not found, data loaded
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { AdminUserEditPage } from "@/components/pages/AdminUserEditPage";

vi.mock("@/lib/admin-api", () => ({
  getUserDetail: vi.fn(),
  updateUser: vi.fn(),
  AdminApiError: class AdminApiError extends Error {
    public status?: number;
    constructor(message: string, status?: number) {
      super(message);
      this.name = "AdminApiError";
      this.status = status;
    }
  },
}));

vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

const mockNavigate = vi.fn();
vi.mock("@tanstack/react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-router")>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

import * as adminApi from "@/lib/admin-api";

const MOCK_USER = {
  id: "user-123",
  name: "João Silva",
  email: "joao@test.com",
  phone: "11999999999",
  type: "PF" as const,
  createdAt: "2024-01-01T00:00:00Z",
  keycloakEnabled: true,
  keycloakEmailVerified: true,
};

beforeEach(() => vi.clearAllMocks());

describe("AdminUserEditPage", () => {
  it("shows loading skeleton initially", () => {
    vi.mocked(adminApi.getUserDetail).mockImplementation(() => new Promise(() => {}));
    render(<AdminUserEditPage userId="user-123" />);
    expect(screen.getByTestId("edit-loading")).toBeInTheDocument();
  });

  it("shows not-found state on 404 error", async () => {
    const err = new Error("Not found") as Error & { status?: number };
    err.status = 404;
    vi.mocked(adminApi.getUserDetail).mockRejectedValue(err);
    render(<AdminUserEditPage userId="user-123" />);
    await waitFor(() => expect(screen.getByTestId("edit-not-found")).toBeInTheDocument());
  });

  it("renders edit form when user loaded", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(MOCK_USER);
    render(<AdminUserEditPage userId="user-123" />);
    await waitFor(() => expect(screen.getByTestId("breadcrumb")).toBeInTheDocument());
    expect(screen.getByText("João Silva")).toBeInTheDocument();
  });

  it("calls getUserDetail with userId on mount", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(MOCK_USER);
    render(<AdminUserEditPage userId="user-abc" />);
    await waitFor(() => {
      expect(adminApi.getUserDetail).toHaveBeenCalledWith("user-abc");
    });
  });

  it("back button on not-found navigates to users list", async () => {
    const err = new Error("Not found") as Error & { status?: number };
    err.status = 404;
    vi.mocked(adminApi.getUserDetail).mockRejectedValue(err);
    render(<AdminUserEditPage userId="user-123" />);
    await waitFor(() => screen.getByTestId("back-to-list-button"));
    screen.getByTestId("back-to-list-button").click();
    expect(mockNavigate).toHaveBeenCalled();
  });
});
