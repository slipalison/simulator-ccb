import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  logoutAdmin,
  getAdminMe,
  getAdministrators,
  AdminLoginError,
  AdminApiError,
  type AdminUserDto,
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

  // ---------------------------------------------------------------------------
  // getAdministrators
  // ---------------------------------------------------------------------------

  describe("getAdministrators", () => {
    it("calls GET /api/admin/administrators with credentials: include and returns AdminUserDto[]", async () => {
      const mockAdmins: AdminUserDto[] = [
        {
          id: "abc-123",
          email: "admin@onboarding.local",
          fullName: "Admin Principal",
          isEnabled: true,
          hasTemporaryPassword: false,
        },
        {
          id: "def-456",
          email: "novo@onboarding.local",
          fullName: "Novo Admin",
          isEnabled: true,
          hasTemporaryPassword: true,
        },
      ];

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockAdmins),
      });

      const result = await getAdministrators();

      expect(mockFetch).toHaveBeenCalledTimes(1);
      expect(mockFetch).toHaveBeenCalledWith(
        "/api/admin/administrators",
        expect.objectContaining({
          method: "GET",
          credentials: "include",
        })
      );
      expect(result).toHaveLength(2);
      expect(result[0].email).toBe("admin@onboarding.local");
      expect(result[1].hasTemporaryPassword).toBe(true);
    });

    it("throws AdminApiError when response is not ok", async () => {
      mockFetch.mockResolvedValue({ ok: false, status: 503 });

      await expect(getAdministrators()).rejects.toThrow(AdminApiError);
      await expect(getAdministrators()).rejects.toThrow(
        "Falha ao carregar administradores."
      );
    });

    it("returns empty array when backend returns []", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve([]),
      });

      const result = await getAdministrators();

      expect(result).toHaveLength(0);
    });
  });
});
