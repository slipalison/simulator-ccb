import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { AuthProvider, useAuth } from "@/lib/auth-context";

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

describe("AuthContext (ACF)", () => {
  it("starts with isLoading=true, isAuthenticated=false", () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAuth(), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    expect(result.current.auth.isLoading).toBe(true);
    expect(result.current.auth.isAuthenticated).toBe(false);
  });

  it("restores session on mount when valid session exists", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          userName: "Test User",
          email: "test@example.com",
          isAuthenticated: true,
        }),
    });

    const { result } = renderHook(() => useAuth(), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(result.current.auth.isAuthenticated).toBe(true);
    expect(result.current.auth.userName).toBe("Test User");
    expect(result.current.auth.email).toBe("test@example.com");
  });

  it("remains unauthenticated when session restoration fails", async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAuth(), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(result.current.auth.isAuthenticated).toBe(false);
    expect(result.current.auth.userName).toBeNull();
    expect(result.current.auth.email).toBeNull();
  });

  it("login redirects to /auth/login", async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAuth(), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    act(() => {
      result.current.login();
    });

    expect(window.location.href).toBe("/auth/login");
  });

  it("logout redirects to /auth/logout", async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const { result } = renderHook(() => useAuth(), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    act(() => {
      result.current.logout();
    });

    expect(window.location.href).toBe("/auth/logout");
  });
});
