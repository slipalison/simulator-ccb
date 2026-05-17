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
    public status?: number;
    constructor(message: string, status?: number) {
      super(message);
      this.name = "AdminApiError";
      this.status = status;
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
      adminId: "admin-id-1",
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
      adminId: "admin-id-1",
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
      adminId: "admin-id-2",
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

  // T-6b: single bounded retry — first /auth/me returns 401, retry returns 200 → authenticated.
  // adminApi.AdminApiError is the mocked class that admin-auth-context.tsx also receives via the
  // same vi.mock factory — instanceof works because both sides share the same class reference.
  // waitFor timeout is raised to 2000ms to accommodate the 200ms retry delay.
  it(
    "tryRestore retries once on 401 and authenticates on successful retry",
    async () => {
      vi.mocked(adminApi.getAdminMe)
        // First call: 401 (post-redirect cookie-commit race)
        .mockRejectedValueOnce(new adminApi.AdminApiError("Session invalid", 401))
        // Second call (after 200ms): success
        .mockResolvedValueOnce({
          adminName: "Admin User",
          adminEmail: "admin@onboarding.local",
          adminId: "admin-id-retry",
        });

      const { result } = renderHook(() => useAdminAuth(), { wrapper });

      await waitFor(
        () => { expect(result.current.admin.isLoading).toBe(false); },
        { timeout: 2000 }
      );

      expect(result.current.admin.isAuthenticated).toBe(true);
      expect(result.current.admin.adminName).toBe("Admin User");
      expect(adminApi.getAdminMe).toHaveBeenCalledTimes(2);
    },
    2500
  );

  // T-6b: single bounded retry — first 401, retry also 401 → not authenticated (no infinite loop)
  it(
    "tryRestore retries once on 401 and finalizes as unauthenticated on second 401",
    async () => {
      vi.mocked(adminApi.getAdminMe)
        .mockRejectedValueOnce(new adminApi.AdminApiError("Session invalid", 401))
        .mockRejectedValueOnce(new adminApi.AdminApiError("Session invalid", 401));

      const { result } = renderHook(() => useAdminAuth(), { wrapper });

      await waitFor(
        () => { expect(result.current.admin.isLoading).toBe(false); },
        { timeout: 2000 }
      );

      expect(result.current.admin.isAuthenticated).toBe(false);
      // Exactly 2 calls: initial attempt + single retry — no further retries
      expect(adminApi.getAdminMe).toHaveBeenCalledTimes(2);
    },
    2500
  );

  // T-6b: 5xx error → fails fast without retry (no 200ms delay)
  it("tryRestore does not retry on 5xx error", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(
      new adminApi.AdminApiError("Server error", 500)
    );

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    expect(result.current.admin.isAuthenticated).toBe(false);
    // Only 1 call — 5xx does not trigger retry
    expect(adminApi.getAdminMe).toHaveBeenCalledTimes(1);
  });
});
