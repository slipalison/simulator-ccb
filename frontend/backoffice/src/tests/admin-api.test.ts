import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  getAdminMe,
  getAdministrators,
  deleteUser,
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
  // getAdminMe
  // ---------------------------------------------------------------------------

  describe("getAdminMe", () => {
    it("calls GET /auth/me with credentials: include", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            adminName: "Test Admin",
            email: "test@onboarding.local",
            isAuthenticated: true,
          }),
      });

      const result = await getAdminMe();

      expect(mockFetch).toHaveBeenCalledWith(
        "/auth/me",
        expect.objectContaining({
          method: "GET",
          credentials: "include",
        })
      );
      expect(result).toEqual({
        adminName: "Test Admin",
        email: "test@onboarding.local",
        isAuthenticated: true,
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

  // ---------------------------------------------------------------------------
  // deleteUser
  // ---------------------------------------------------------------------------

  describe("deleteUser", () => {
    it("sends DELETE with confirmEmail body and credentials: include", async () => {
      mockFetch.mockResolvedValueOnce({ ok: true, status: 204 });

      await deleteUser("user-123", "target@example.com");

      expect(mockFetch).toHaveBeenCalledWith(
        "/api/admin/users/user-123",
        expect.objectContaining({
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ confirmEmail: "target@example.com" }),
          credentials: "include",
        })
      );
    });

    it("throws AdminApiError with 400 when confirmEmail is invalid", async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: () => Promise.resolve({ detail: "Email de confirmacao invalido." }),
      });

      await expect(deleteUser("user-123", "wrong@email.com")).rejects.toThrow(
        AdminApiError
      );
    });

    it("throws AdminApiError with 404 when user not found", async () => {
      mockFetch.mockResolvedValueOnce({ ok: false, status: 404 });

      await expect(deleteUser("nonexistent", "x@y.com")).rejects.toThrow(
        AdminApiError
      );
    });
  });
});
