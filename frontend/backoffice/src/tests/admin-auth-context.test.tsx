import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { AdminAuthProvider, useAdminAuth } from "@/lib/admin-auth-context";
import * as adminApi from "@/lib/admin-api";

// Mock the admin API module
vi.mock("@/lib/admin-api", () => ({

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
  return <AdminAuthProvider>{children}</AdminAuthProvider>;
}

describe("AdminAuthContext", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("starts with isLoading=true, isAuthenticated=false", () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    expect(result.current.admin.isLoading).toBe(true);
    expect(result.current.admin.isAuthenticated).toBe(false);
  });

  it("restores session on mount when valid session exists", async () => {
    vi.mocked(adminApi.getAdminMe).mockResolvedValue({
      adminName: "Admin User",
      adminEmail: "admin@onboarding.local",
    });

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    expect(result.current.admin.isAuthenticated).toBe(true);
    expect(result.current.admin.adminName).toBe("Admin User");
    expect(result.current.admin.adminEmail).toBe("admin@onboarding.local");
  });

  it("remains unauthenticated when session restoration fails", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    expect(result.current.admin.isAuthenticated).toBe(false);
    expect(result.current.admin.adminName).toBeNull();
    expect(result.current.admin.adminEmail).toBeNull();
  });


  it("logout clears all state", async () => {
    // Session restoration succeeds first
    vi.mocked(adminApi.getAdminMe).mockResolvedValueOnce({
      adminName: "Admin User",
      adminEmail: "admin@onboarding.local",
    });

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
      expect(result.current.admin.isAuthenticated).toBe(true);
    });

    // Mock logout API
    vi.mocked(adminApi.logoutAdmin).mockResolvedValue();

    // Logout
    await act(async () => {
      await result.current.logout();
    });

    expect(result.current.admin.isAuthenticated).toBe(false);
    expect(result.current.admin.adminName).toBeNull();
    expect(result.current.admin.adminEmail).toBeNull();
  });

  it("restoreSession returns true when session is valid", async () => {
    // Initial mount: session restoration fails
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    // Manual restore: succeeds
    vi.mocked(adminApi.getAdminMe).mockResolvedValueOnce({
      adminName: "Restored Admin",
      adminEmail: "restored@onboarding.local",
    });

    const restored = await act(async () => {
      return await result.current.restoreSession();
    });

    expect(restored).toBe(true);
    expect(result.current.admin.isAuthenticated).toBe(true);
    expect(result.current.admin.adminName).toBe("Restored Admin");
  });

  it("restoreSession returns false when session is invalid", async () => {
    // Initial mount: session restoration fails
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    // Manual restore: also fails
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    const restored = await act(async () => {
      return await result.current.restoreSession();
    });

    expect(restored).toBe(false);
    expect(result.current.admin.isAuthenticated).toBe(false);
  });
});
