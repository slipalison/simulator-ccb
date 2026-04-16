import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { AdminLayout } from "@/components/templates/AdminLayout";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import { Toaster } from "@/components/ui/sonner";

// Mock fetch
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock window.location
const originalLocation = window.location;
beforeEach(() => {
  vi.clearAllMocks();
  mockFetch.mockReset();
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
  it("renders header with admin name", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          adminName: "Test Admin",
          email: "test@onboarding.local",
          isAuthenticated: true,
        }),
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

  it("shows logout button and redirects on click", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          adminName: "Test Admin",
          email: "test@onboarding.local",
          isAuthenticated: true,
        }),
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

  it("sidebar has link to /admin/users", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          adminName: "Test Admin",
          email: "test@onboarding.local",
          isAuthenticated: true,
        }),
    });

    render(
      wrapper({
        children: <AdminLayout><p>Content</p></AdminLayout>,
      })
    );

    await waitFor(() => {
      expect(screen.getByTestId("admin-sidebar")).toBeInTheDocument();
    });

    const usersLink = screen.getByTestId("sidebar-users-link");
    expect(usersLink).toBeInTheDocument();
    expect(usersLink.getAttribute("href")).toBe("/admin/users");
  });
});
