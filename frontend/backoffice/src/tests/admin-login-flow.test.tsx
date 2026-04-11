import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AdminLoginPage } from "@/components/pages/AdminLoginPage";
import { AdminAuthProvider, useAdminAuth } from "@/lib/admin-auth-context";
import * as adminApi from "@/lib/admin-api";
import { RouterProvider, createRouter, createMemoryHistory } from "@tanstack/react-router";
import { router } from "@/router";
import { Toaster } from "@/components/ui/sonner";

// Mock admin API
vi.mock("@/lib/admin-api", () => ({
  loginAdmin: vi.fn(),
  logoutAdmin: vi.fn(),
  getAdminMe: vi.fn(),
  AdminLoginError: class AdminLoginError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "AdminLoginError";
    }
  },
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

  it("renders admin login form with email and password fields", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

    render(wrapper({ children: <AdminLoginPage /> }));

    await waitFor(() => {
      expect(screen.getByText(/admin backoffice/i)).toBeInTheDocument();
    });

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/senha/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
  });

  it("submits form and redirects to /admin/users on success", async () => {
    // Initial mount: no session
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));
    // Login: success
    vi.mocked(adminApi.loginAdmin).mockResolvedValue({
      adminName: "Admin User",
      adminEmail: "admin@onboarding.local",
    });

    const { memoryHistory } = await renderWithRouter(["/admin/login"]);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    // Fill form
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "admin@onboarding.local" } });
      fireEvent.change(passwordInput, { target: { value: "SecureP@ss123" } });
    });

    // Submit
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    // Wait for redirect to /admin/users
    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/admin/users");
    });
  });

  it("shows validation error for invalid email", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    render(wrapper({ children: <AdminLoginPage /> }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    // Fill with invalid email, submit
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "not-an-email" } });
      fireEvent.change(passwordInput, { target: { value: "somepass" } });
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    await waitFor(() => {
      expect(screen.getByText(/email invalido/i)).toBeInTheDocument();
    });

    expect(adminApi.loginAdmin).not.toHaveBeenCalled();
  });

  it("shows validation error for empty password", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    render(wrapper({ children: <AdminLoginPage /> }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    // Only fill email, leave password empty
    const emailInput = screen.getByLabelText(/email/i);
    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "admin@onboarding.local" } });
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    await waitFor(() => {
      const errors = screen.getAllByText(/obrigator/i);
      expect(errors.length).toBeGreaterThan(0);
    });

    expect(adminApi.loginAdmin).not.toHaveBeenCalled();
  });

  it("shows error toast when login fails", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));
    vi.mocked(adminApi.loginAdmin).mockRejectedValue(
      new adminApi.AdminLoginError("Credenciais invalidas.")
    );

    render(wrapper({ children: <AdminLoginPage /> }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
    });

    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/senha/i);

    await act(async () => {
      fireEvent.change(emailInput, { target: { value: "admin@onboarding.local" } });
      fireEvent.change(passwordInput, { target: { value: "wrongpass" } });
      fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    });

    await waitFor(() => {
      expect(screen.getByText(/credenciais invalidas/i)).toBeInTheDocument();
    });
  });

  it("redirects to /admin/users if already authenticated", async () => {
    // Session restoration succeeds
    vi.mocked(adminApi.getAdminMe).mockResolvedValue({
      adminName: "Admin User",
      adminEmail: "admin@onboarding.local",
    });

    const { memoryHistory } = await renderWithRouter(["/admin/login"]);

    await waitFor(() => {
      expect(memoryHistory.location.pathname).toBe("/admin/users");
    });
  });
});
