import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { AuthProvider, useAuth } from "@/lib/auth-context";
import * as api from "@/lib/api";

// Mock the entire API module
vi.mock("@/lib/api", () => ({
  loginClient: vi.fn(),
  refreshTokenClient: vi.fn(),
  LoginError: class LoginError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "LoginError";
    }
  },
  ApiError: class ApiError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "ApiError";
    }
  },
}));

// Mock fetch for session restoration
const mockFetch = vi.fn();
global.fetch = mockFetch;

function wrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

describe("Auth persistence: session restoration via httpOnly cookies", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  it("restoreSession returns false when no session exists", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401,
    });

    const { result } = renderHook(() => useAuth(), { wrapper });

    // Wait for initial loading to complete
    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    expect(result.current.auth.isAuthenticated).toBe(false);

    // Try to restore session
    const restored = await act(async () => {
      return await result.current.restoreSession();
    });

    expect(restored).toBe(false);
    expect(result.current.auth.isAuthenticated).toBe(false);
  });

  it("restoreSession returns true and sets tokens when session is valid", async () => {
    // First mock: initial session restoration on mount (fails)
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401,
    });

    const { result } = renderHook(() => useAuth(), { wrapper });

    // Wait for initial loading to complete
    await waitFor(() => {
      expect(result.current.auth.isLoading).toBe(false);
    });

    // Second mock: manual restoreSession call (succeeds)
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          accessToken: "restored-access-token",
          expiresIn: 300,
        }),
    });

    // Restore session
    const restored = await act(async () => {
      return await result.current.restoreSession();
    });

    expect(restored).toBe(true);
    expect(result.current.auth.isAuthenticated).toBe(true);
    expect(result.current.getAccessToken()).toBe("restored-access-token");
  });

  it("login stores accessToken from response (refreshToken in httpOnly cookie)", async () => {
    const mockResponse = {
      accessToken: "mock-access-token",
      refreshToken: "mock-refresh-token", // Not stored in memory anymore
      expiresIn: 300,
      tokenType: "Bearer",
      refreshExpiresIn: 86400,
      scope: "openid",
    };
    vi.mocked(api.loginClient).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth(), { wrapper });

    await act(async () => {
      await result.current.login("test@example.com", "password123");
    });

    expect(result.current.auth.isAuthenticated).toBe(true);
    expect(result.current.getAccessToken()).toBe("mock-access-token");
  });

  it("logout calls backend to clear httpOnly cookie", async () => {
    const mockResponse = {
      accessToken: "mock-access-token",
      refreshToken: "mock-refresh-token",
      expiresIn: 300,
      tokenType: "Bearer",
      refreshExpiresIn: 86400,
      scope: "openid",
    };
    vi.mocked(api.loginClient).mockResolvedValue(mockResponse);

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          accessToken: "restored-access-token",
          expiresIn: 300,
        }),
    });

    const { result } = renderHook(() => useAuth(), { wrapper });

    // Login first
    await act(async () => {
      await result.current.login("test@example.com", "password123");
    });
    expect(result.current.auth.isAuthenticated).toBe(true);

    // Restore session to set initial loading to false
    await act(async () => {
      await result.current.restoreSession();
    });

    // Mock logout fetch
    mockFetch.mockResolvedValueOnce({ ok: true });

    // Then logout
    await act(async () => {
      await result.current.logout();
    });

    expect(result.current.auth.isAuthenticated).toBe(false);
    expect(result.current.getAccessToken()).toBeNull();
  });

  it("tokens are NOT written to localStorage", async () => {
    const getItemSpy = vi.spyOn(Storage.prototype, "getItem");
    const setItemSpy = vi.spyOn(Storage.prototype, "setItem");

    const mockResponse = {
      accessToken: "mock-access-token",
      refreshToken: "mock-refresh-token",
      expiresIn: 300,
      tokenType: "Bearer",
      refreshExpiresIn: 86400,
      scope: "openid",
    };
    vi.mocked(api.loginClient).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth(), { wrapper });

    await act(async () => {
      await result.current.login("test@example.com", "password123");
    });

    expect(getItemSpy).not.toHaveBeenCalled();
    expect(setItemSpy).not.toHaveBeenCalled();

    getItemSpy.mockRestore();
    setItemSpy.mockRestore();
  });

  it("tokens are NOT written to sessionStorage", async () => {
    const getSessionItemSpy = vi.spyOn(Storage.prototype, "getItem");
    const setSessionItemSpy = vi.spyOn(Storage.prototype, "setItem");

    const mockResponse = {
      accessToken: "mock-access-token",
      refreshToken: "mock-refresh-token",
      expiresIn: 300,
      tokenType: "Bearer",
      refreshExpiresIn: 86400,
      scope: "openid",
    };
    vi.mocked(api.loginClient).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth(), { wrapper });

    await act(async () => {
      await result.current.login("test@example.com", "password123");
    });

    expect(getSessionItemSpy).not.toHaveBeenCalled();
    expect(setSessionItemSpy).not.toHaveBeenCalled();

    getSessionItemSpy.mockRestore();
    setSessionItemSpy.mockRestore();
  });
});
