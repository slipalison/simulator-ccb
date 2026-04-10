import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  loginAdmin,
  logoutAdmin,
  getAdminMe,
  AdminLoginError,
  AdminApiError,
} from "@/lib/admin-api";

// ---------------------------------------------------------------------------
// Mock fetch
// ---------------------------------------------------------------------------

const mockFetch = vi.fn();
global.fetch = mockFetch;

describe("admin-api.ts", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  // ---------------------------------------------------------------------------
  // loginAdmin
  // ---------------------------------------------------------------------------

  describe("loginAdmin", () => {
    it("calls POST /api/admin/auth/login with credentials: include", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            adminName: "Admin User",
            adminEmail: "admin@onboarding.local",
          }),
      });

      const result = await loginAdmin("admin@onboarding.local", "SecureP@ss123");

      expect(mockFetch).toHaveBeenCalledTimes(1);
      expect(mockFetch).toHaveBeenCalledWith(
        "/api/admin/auth/login",
        expect.objectContaining({
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            email: "admin@onboarding.local",
            password: "SecureP@ss123",
          }),
          credentials: "include",
        })
      );
      expect(result).toEqual({
        adminName: "Admin User",
        adminEmail: "admin@onboarding.local",
      });
    });

    it("throws AdminLoginError on 401", async () => {
      mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

      await expect(
        loginAdmin("wrong@bad.com", "wrong")
      ).rejects.toThrow(AdminLoginError);
    });

    it("throws AdminApiError on other failures", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.resolve({ detail: "Internal error" }),
      });

      await expect(
        loginAdmin("admin@onboarding.local", "pass")
      ).rejects.toThrow(AdminApiError);
    });
  });

  // ---------------------------------------------------------------------------
  // logoutAdmin
  // ---------------------------------------------------------------------------

  describe("logoutAdmin", () => {
    it("calls POST /api/admin/auth/logout with credentials: include", async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await logoutAdmin();

      expect(mockFetch).toHaveBeenCalledWith(
        "/api/admin/auth/logout",
        expect.objectContaining({
          method: "POST",
          credentials: "include",
        })
      );
    });

    it("throws AdminApiError on failure", async () => {
      mockFetch.mockResolvedValueOnce({ ok: false, status: 500 });

      await expect(logoutAdmin()).rejects.toThrow(AdminApiError);
    });
  });

  // ---------------------------------------------------------------------------
  // getAdminMe
  // ---------------------------------------------------------------------------

  describe("getAdminMe", () => {
    it("calls GET /api/admin/auth/me with credentials: include", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            adminName: "Test Admin",
            adminEmail: "test@onboarding.local",
          }),
      });

      const result = await getAdminMe();

      expect(mockFetch).toHaveBeenCalledWith(
        "/api/admin/auth/me",
        expect.objectContaining({
          method: "GET",
          credentials: "include",
        })
      );
      expect(result).toEqual({
        adminName: "Test Admin",
        adminEmail: "test@onboarding.local",
      });
    });

    it("throws AdminApiError on failure", async () => {
      mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

      await expect(getAdminMe()).rejects.toThrow(AdminApiError);
    });
  });
});
