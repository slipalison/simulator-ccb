import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { AdminUserDetailPage } from "@/components/pages/AdminUserDetailPage";
import * as adminApi from "@/lib/admin-api";

// Mock the admin-api module
vi.mock("@/lib/admin-api", () => ({
  getUserDetail: vi.fn(),
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

describe("Admin User Detail Page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders with loading state on mount", async () => {
    vi.mocked(adminApi.getUserDetail).mockImplementation(
      () => new Promise(() => {})
    );

    render(<AdminUserDetailPage userId="1" />);

    expect(screen.getByTestId("detail-loading")).toBeInTheDocument();
  });

  it("fetches user and displays detail", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("user-detail-card")).toBeInTheDocument();
    });

    expect(screen.getByTestId("user-name")).toHaveTextContent("Joao Silva");
    expect(screen.getByTestId("user-email")).toHaveTextContent("joao@example.com");
    expect(screen.getByTestId("user-type")).toHaveTextContent("Pessoa Fisica");
    expect(screen.getByTestId("user-document")).toHaveTextContent("123.456.789-00");
  });

  it("calls getUserDetail with correct userId", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="user-123" />);

    await waitFor(() => {
      expect(adminApi.getUserDetail).toHaveBeenCalledWith("user-123");
    });
  });

  it("shows 404 state when user not found", async () => {
    vi.mocked(adminApi.getUserDetail).mockRejectedValue(
      new adminApi.AdminApiError("Usuario nao encontrado.", 404)
    );

    render(<AdminUserDetailPage userId="nonexistent" />);

    await waitFor(() => {
      expect(screen.getByTestId("detail-not-found")).toBeInTheDocument();
    });

    expect(screen.getByText("Usuario nao encontrado")).toBeInTheDocument();
    expect(screen.getByTestId("back-to-list-button")).toBeInTheDocument();
  });

  it("shows error state with retry button on API failure", async () => {
    vi.mocked(adminApi.getUserDetail).mockRejectedValue(
      new adminApi.AdminApiError("Falha ao carregar dados do usuario.")
    );

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("detail-error")).toBeInTheDocument();
    });

    expect(screen.getByText("Erro ao carregar")).toBeInTheDocument();
    expect(screen.getByTestId("retry-button")).toBeInTheDocument();
  });

  it("retry button re-fetches user", async () => {
    vi.mocked(adminApi.getUserDetail)
      .mockRejectedValueOnce(new adminApi.AdminApiError("Error"))
      .mockResolvedValueOnce(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("retry-button")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("retry-button"));

    await waitFor(() => {
      expect(screen.getByTestId("user-detail-card")).toBeInTheDocument();
    });

    expect(adminApi.getUserDetail).toHaveBeenCalledTimes(2);
  });

  it("back button navigates to /admin/users", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("breadcrumb-back")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("breadcrumb-back"));

    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/users" });
  });

  it("back-to-list button on 404 navigates to /admin/users", async () => {
    vi.mocked(adminApi.getUserDetail).mockRejectedValue(
      new adminApi.AdminApiError("Usuario nao encontrado.", 404)
    );

    render(<AdminUserDetailPage userId="nonexistent" />);

    await waitFor(() => {
      expect(screen.getByTestId("back-to-list-button")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("back-to-list-button"));

    expect(mockNavigate).toHaveBeenCalledWith({ to: "/admin/users" });
  });

  it("shows breadcrumb with user name", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("breadcrumb-name")).toHaveTextContent("Joao Silva");
    });
  });

  it("shows action buttons with edit navigating to edit page", async () => {
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockUserDetail);

    render(<AdminUserDetailPage userId="1" />);

    await waitFor(() => {
      expect(screen.getByTestId("edit-button")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("edit-button"));

    expect(mockNavigate).toHaveBeenCalledWith({
      to: "/admin/users/$id/edit",
      params: { id: "1" },
    });
  });

  it("displays PJ user data correctly", async () => {
    const mockPJUser = {
      ...mockUserDetail,
      type: "PJ" as const,
      document: "12.345.678/0001-99",
      razaoSocial: "Acme Corporation Ltda",
    };
    vi.mocked(adminApi.getUserDetail).mockResolvedValue(mockPJUser);

    render(<AdminUserDetailPage userId="2" />);

    await waitFor(() => {
      expect(screen.getByTestId("user-type")).toHaveTextContent("Pessoa Juridica");
    });

    expect(screen.getByTestId("pj-info")).toBeInTheDocument();
    expect(screen.getByTestId("user-razao-social")).toHaveTextContent("Acme Corporation Ltda");
  });
});
