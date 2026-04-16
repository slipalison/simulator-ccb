import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AdminAdministratorsPage } from "@/components/pages/AdminAdministratorsPage";
import * as adminApi from "@/lib/admin-api";

vi.mock("@/lib/admin-api", () => ({
  getAdministrators: vi.fn(),
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

const mockAdmins: adminApi.AdminUserDto[] = [
  { id: "1", email: "admin1@test.com", fullName: "Admin One", isEnabled: true, hasTemporaryPassword: false },
  { id: "2", email: "admin2@test.com", fullName: "Admin Two", isEnabled: false, hasTemporaryPassword: true },
];

describe("AdminAdministratorsPage", () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it("renders loading state initially", () => {
    vi.mocked(adminApi.getAdministrators).mockImplementation(() => new Promise(() => {}));
    render(<AdminAdministratorsPage />);
    expect(screen.getByTestId("loading-state")).toBeInTheDocument();
  });

  it("renders administrators table with data", async () => {
    vi.mocked(adminApi.getAdministrators).mockResolvedValue(mockAdmins);
    render(<AdminAdministratorsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("administrators-table")).toBeInTheDocument();
    });
    expect(screen.getByText("Admin One")).toBeInTheDocument();
    expect(screen.getByText("admin1@test.com")).toBeInTheDocument();
    expect(screen.getByText("Admin Two")).toBeInTheDocument();
    expect(screen.getByText("admin2@test.com")).toBeInTheDocument();
  });

  it("shows active/blocked badges based on isEnabled", async () => {
    vi.mocked(adminApi.getAdministrators).mockResolvedValue(mockAdmins);
    render(<AdminAdministratorsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("administrators-table")).toBeInTheDocument();
    });
    expect(screen.getAllByTestId("badge-active")).toHaveLength(1);
    expect(screen.getByTestId("badge-blocked")).toBeInTheDocument();
  });

  it("shows temp password badges based on hasTemporaryPassword", async () => {
    vi.mocked(adminApi.getAdministrators).mockResolvedValue(mockAdmins);
    render(<AdminAdministratorsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("administrators-table")).toBeInTheDocument();
    });
    expect(screen.getByTestId("badge-temp-password")).toBeInTheDocument();
    expect(screen.getByTestId("badge-password-set")).toBeInTheDocument();
  });

  it("shows empty state when no administrators", async () => {
    vi.mocked(adminApi.getAdministrators).mockResolvedValue([]);
    render(<AdminAdministratorsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("empty-state")).toBeInTheDocument();
    });
  });

  it("shows error state and retry button on failure", async () => {
    vi.mocked(adminApi.getAdministrators).mockRejectedValue(new Error("Network error"));
    render(<AdminAdministratorsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("error-state")).toBeInTheDocument();
    });
    expect(screen.getByRole("button", { name: /tentar novamente/i })).toBeInTheDocument();
  });

  it("retry button refetches data", async () => {
    vi.mocked(adminApi.getAdministrators)
      .mockRejectedValueOnce(new Error("Network error"))
      .mockResolvedValueOnce(mockAdmins);
    render(<AdminAdministratorsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("error-state")).toBeInTheDocument();
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /tentar novamente/i }));
    });
    await waitFor(() => {
      expect(screen.getByTestId("administrators-table")).toBeInTheDocument();
    });
    expect(adminApi.getAdministrators).toHaveBeenCalledTimes(2);
  });
});
