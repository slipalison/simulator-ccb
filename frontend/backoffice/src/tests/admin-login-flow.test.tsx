import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AdminLoginPage } from "@/components/pages/AdminLoginPage";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import * as adminApi from "@/lib/admin-api";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { router } from "@/router";
import { Toaster } from "@/components/ui/sonner";

// Mock admin API
vi.mock("@/lib/admin-api", () => ({
  logoutAdmin: vi.fn(),
  getAdminMe: vi.fn(),
  AdminApiError: class AdminApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "AdminApiError";
    }
  },
}));

function wrapper({ children }: { children: React.ReactNode }) {
  return (
    <AdminAuthProvider>
      {children}
      <Toaster />
    </AdminAuthProvider>
  );
}

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

async function renderWithRouter(initialEntries: string[] = ["/admin/login"]) {
  const memoryHistory = createMemoryHistory({ initialEntries });
  const testRouter = createRouter({
    routeTree: router.options.routeTree,
    history: memoryHistory,
  });
  const view = render(
    <AdminAuthProvider>
      <RouterProvider router={testRouter} />
      <Toaster />
    </AdminAuthProvider>
  );
  await testRouter.load();
  return { ...view, router: testRouter, memoryHistory };
}

describe("Admin Login Flow", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders admin login page", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

    render(wrapper({ children: <AdminLoginPage /> }));

    await waitFor(() => {
      expect(screen.getByText(/admin backoffice/i)).toBeInTheDocument();
    });

    expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
  });

  it("clicks login button and redirects to /auth/login", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

    render(wrapper({ children: <AdminLoginPage /> }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    expect(window.location.href).toBe("/auth/login");
  });

  it("redirects to /admin/users if already authenticated", async () => {
    vi.mocked(adminApi.getAdminMe).mockResolvedValue({
      adminName: "Admin User",
      adminEmail: "admin@onboarding.local",
      adminId: "admin-user-id",
    });

    const { memoryHistory } = await renderWithRouter(["/admin/login"]);

    await waitFor(() => {
      expect(window.location.href).toBe("/admin/users");
    });
  });
});
