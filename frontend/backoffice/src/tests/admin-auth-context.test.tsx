import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { AdminAuthProvider, useAdminAuth } from "@/lib/admin-auth-context";
import * as adminApi from "@/lib/admin-api";

vi.mock("@/lib/admin-api", () => ({
  getAdminMe: vi.fn(),
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
  let originalLocation: Location;

  beforeEach(() => {
    vi.clearAllMocks();
    originalLocation = window.location;
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

  it("login() redirects to /auth/login", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValue(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    act(() => {
      result.current.login();
    });

    expect(window.location.href).toBe("/auth/login");
  });

  it("logout() redirects to /auth/logout", async () => {
    vi.mocked(adminApi.getAdminMe).mockResolvedValueOnce({
      adminName: "Admin User",
      adminEmail: "admin@onboarding.local",
    });

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
      expect(result.current.admin.isAuthenticated).toBe(true);
    });

    act(() => {
      result.current.logout();
    });

    expect(window.location.href).toBe("/auth/logout");
  });

  it("restoreSession returns true when session is valid", async () => {
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

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
    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    const { result } = renderHook(() => useAdminAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.admin.isLoading).toBe(false);
    });

    vi.mocked(adminApi.getAdminMe).mockRejectedValueOnce(new Error("No session"));

    const restored = await act(async () => {
      return await result.current.restoreSession();
    });

    expect(restored).toBe(false);
    expect(result.current.admin.isAuthenticated).toBe(false);
  });
});
