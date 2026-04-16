import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { AdminAuthProvider, useAdminAuth } from "@/lib/admin-auth-context";

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

describe("AdminAuthContext (ACF)", () => {
  it("starts with isLoading=true, isAuthenticated=false", () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAdminAuth(), {
      wrapper: ({ children }) => <AdminAuthProvider>{children}</AdminAuthProvider>,
    });

    expect(result.current.admin.isLoading).toBe(true);
    expect(result.current.admin.isAuthenticated).toBe(false);
  });

  it("restores session on mount when valid session exists", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          adminName: "Admin User",
          email: "admin@onboarding.local",
          isAuthenticated: true,
        }),
    });

    const { result } = renderHook(() => useAdminAuth(), {
      wrapper: ({ children }) => <AdminAuthProvider>{children}</AdminAuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    expect(result.current.admin.isAuthenticated).toBe(true);
    expect(result.current.admin.adminName).toBe("Admin User");
    expect(result.current.admin.adminEmail).toBe("admin@onboarding.local");
  });

  it("remains unauthenticated when session restoration fails", async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAdminAuth(), {
      wrapper: ({ children }) => <AdminAuthProvider>{children}</AdminAuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    expect(result.current.admin.isAuthenticated).toBe(false);
    expect(result.current.admin.adminName).toBeNull();
    expect(result.current.admin.adminEmail).toBeNull();
  });

  it("login redirects to /auth/login", async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAdminAuth(), {
      wrapper: ({ children }) => <AdminAuthProvider>{children}</AdminAuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    act(() => {
      result.current.login();
    });

    expect(window.location.href).toBe("/auth/login");
  });

  it("logout redirects to /auth/logout", async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAdminAuth(), {
      wrapper: ({ children }) => <AdminAuthProvider>{children}</AdminAuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    act(() => {
      result.current.logout();
    });

    expect(window.location.href).toBe("/auth/logout");
  });
});
