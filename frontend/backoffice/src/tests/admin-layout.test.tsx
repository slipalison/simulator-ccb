import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import * as adminApi from "@/lib/admin-api";
import { Toaster } from "@/components/ui/sonner";

// Mock admin API
vi.mock("@/lib/admin-api", () => ({
  getAdminMe: vi.fn(),
  AdminApiError: class AdminApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "AdminApiError";
    }
  },
}));

// Mock window.location
const originalLocation = window.location;
beforeEach(() => {
  Object.defineProperty(window, "location", {
    writable: true,
    value: { href: "" },
  });
});
afterEach(() => {
  Object.defineProperty(window, "location", {
    writable: true,
    value: originalLocation,
  });
});

function wrapper({ children }: { children: React.ReactNode }) {
  return (
    <AdminAuthProvider>
      {children}
      <Toaster />
    </AdminAuthProvider>
  );
}

describe("Admin Layout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders header with admin name", async () => {
    vi.mocked(adminApi.getAdminMe).mockResolvedValue({
      adminName: "Test Admin",
      adminEmail: "test@onboarding.local",
    });

    render(
      wrapper({
        children: <AdminLayout><p data-testid="admin-content">Content</p></AdminLayout>,
      })
    );

    await waitFor(() => {
      expect(screen.getByTestId("admin-greeting")).toHaveTextContent("Ola, Test Admin");
    });

    expect(screen.getByTestId("admin-layout")).toBeInTheDocument();
    expect(screen.getByText(/backoffice admin/i)).toBeInTheDocument();
  });

  it("shows logout button and redirects to /auth/logout on click", async () => {
    vi.mocked(adminApi.getAdminMe).mockResolvedValue({
      adminName: "Test Admin",
      adminEmail: "test@onboarding.local",
    });

    render(
      wrapper({
        children: <AdminLayout><p>Content</p></AdminLayout>,
      })
    );

    await waitFor(() => {
      expect(screen.getByTestId("admin-logout-button")).toBeInTheDocument();
    });

    await act(async () => {
      fireEvent.click(screen.getByTestId("admin-logout-button"));
    });

    // After logout, window.location.href should be set to /auth/logout
    await waitFor(() => {
      expect(window.location.href).toBe("/auth/logout");
    });
  });
});
