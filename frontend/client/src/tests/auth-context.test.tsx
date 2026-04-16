import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { AuthProvider, useAuth } from "@/lib/auth-context";

// Mock fetch for /auth/me calls
const mockFetch = vi.fn();
global.fetch = mockFetch;

function wrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

describe("AuthContext — ACF redirect-based auth", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
    // Default: /auth/me returns 401 (not authenticated)
    mockFetch.mockResolvedValue({ ok: false, status: 401 });
  });

  it("starts loading and resolves to unauthenticated when /auth/me returns 401", async () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.auth.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(result.current.auth.isAuthenticated).toBe(false);
    expect(result.current.auth.userName).toBeNull();
    expect(result.current.auth.email).toBeNull();
  });

  it("sets isAuthenticated + userName + email when /auth/me returns 200", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          isAuthenticated: true,
          userName: "João Silva",
          email: "joao@example.com",
          sub: "user-123",
        }),
    });

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(result.current.auth.isAuthenticated).toBe(true);
    expect(result.current.auth.userName).toBe("João Silva");
    expect(result.current.auth.email).toBe("joao@example.com");
  });

  it("login() redirects to /auth/login", async () => {
    const assignSpy = vi.fn();
    Object.defineProperty(window, "location", {
      value: { href: "" },
      writable: true,
    });
    Object.defineProperty(window.location, "href", {
      set: assignSpy,
      configurable: true,
    });

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    act(() => {
      result.current.login();
    });

    expect(assignSpy).toHaveBeenCalledWith("/auth/login");
  });

  it("logout() redirects to /auth/logout", async () => {
    const assignSpy = vi.fn();
    Object.defineProperty(window, "location", {
      value: { href: "" },
      writable: true,
    });
    Object.defineProperty(window.location, "href", {
      set: assignSpy,
      configurable: true,
    });

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    act(() => {
      result.current.logout();
    });

    expect(assignSpy).toHaveBeenCalledWith("/auth/logout");
  });

  it("tokens are NOT written to localStorage", async () => {
    const getItemSpy = vi.spyOn(Storage.prototype, "getItem");
    const setItemSpy = vi.spyOn(Storage.prototype, "setItem");

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(getItemSpy).not.toHaveBeenCalled();
    expect(setItemSpy).not.toHaveBeenCalled();

    getItemSpy.mockRestore();
    setItemSpy.mockRestore();
  });

  it("handles /auth/me fetch error gracefully", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network error"));

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(result.current.auth.isAuthenticated).toBe(false);
  });
});
