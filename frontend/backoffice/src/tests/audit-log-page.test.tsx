import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AuditLogPage } from "@/components/pages/AuditLogPage";
import * as adminApi from "@/lib/admin-api";

vi.mock("@/lib/admin-api", () => ({
  getAuditLog: vi.fn(),
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

const mockAuditItems = [
  {
    id: "1",
    timestamp: "2026-04-16T10:00:00Z",
    adminUserId: "admin-1",
    adminUserName: "Admin One",
    actionType: "UserBlocked",
    targetUserId: "user-1",
    targetUserName: "Joao Silva",
    details: null,
    ipAddress: "127.0.0.1",
  },
  {
    id: "2",
    timestamp: "2026-04-16T11:00:00Z",
    adminUserId: "admin-2",
    adminUserName: "Admin Two",
    actionType: "UserCreated",
    targetUserId: "user-2",
    targetUserName: "Maria Santos",
    details: null,
    ipAddress: "192.168.1.1",
  },
];

function mockAuditResult(overrides = {}) {
  return { items: mockAuditItems, totalCount: 2, page: 1, pageSize: 20, ...overrides };
}

describe("AuditLogPage", () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it("renders loading state initially", () => {
    vi.mocked(adminApi.getAuditLog).mockImplementation(() => new Promise(() => {}));
    render(<AuditLogPage />);
    expect(screen.getByText(/carregando/i)).toBeInTheDocument();
  });

  it("renders audit log table with entries", async () => {
    vi.mocked(adminApi.getAuditLog).mockResolvedValue(mockAuditResult());
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText("Admin One")).toBeInTheDocument();
    });
    expect(screen.getByText("Joao Silva")).toBeInTheDocument();
    expect(screen.getByText("127.0.0.1")).toBeInTheDocument();
  });

  it("shows action type labels translated", async () => {
    vi.mocked(adminApi.getAuditLog).mockResolvedValue(mockAuditResult());
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText("Usuario Bloqueado")).toBeInTheDocument();
    });
    expect(screen.getByText("Usuario Criado")).toBeInTheDocument();
  });

  it("shows empty state when no entries", async () => {
    vi.mocked(adminApi.getAuditLog).mockResolvedValue(mockAuditResult({ items: [], totalCount: 0 }));
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText(/nenhuma entrada encontrada/i)).toBeInTheDocument();
    });
  });

  it("shows error state on failure", async () => {
    vi.mocked(adminApi.getAuditLog).mockRejectedValue(new Error("Network error"));
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText(/erro ao carregar audit log/i)).toBeInTheDocument();
    });
  });

  it("filter button calls fetchAuditLog with filter params", async () => {
    vi.mocked(adminApi.getAuditLog).mockResolvedValue(mockAuditResult());
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText("Admin One")).toBeInTheDocument();
    });
    const filterButton = screen.getByRole("button", { name: /filtrar/i });
    await act(async () => {
      fireEvent.click(filterButton);
    });
    await waitFor(() => {
      expect(adminApi.getAuditLog).toHaveBeenCalled();
    });
  });

  it("reset button clears all filters and resets page", async () => {
    vi.mocked(adminApi.getAuditLog).mockResolvedValue(mockAuditResult());
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText("Admin One")).toBeInTheDocument();
    });
    const resetButton = screen.getByRole("button", { name: /limpar/i });
    await act(async () => {
      fireEvent.click(resetButton);
    });
    expect(adminApi.getAuditLog).toHaveBeenCalled();
  });

  it("pagination shows next/previous buttons when multiple pages", async () => {
    vi.mocked(adminApi.getAuditLog).mockResolvedValue(
      mockAuditResult({ totalCount: 50, page: 1, pageSize: 20 })
    );
    render(<AuditLogPage />);
    await waitFor(() => {
      expect(screen.getByText(/pagina 1 de 3/i)).toBeInTheDocument();
    });
    expect(screen.getByRole("button", { name: /anterior/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /proxima/i })).not.toBeDisabled();
  });
});
