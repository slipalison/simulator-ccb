// ---------------------------------------------------------------------------
// GREEN tests — Profile components and API client
// ---------------------------------------------------------------------------
// Updated in plan 40-01: PJ-only profile with CompanyProfileDto and group badges.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { ProfileField } from "@/components/atoms/ProfileField";
import { ProfileBadge } from "@/components/atoms/ProfileBadge";
import { ProfileCard } from "@/components/molecules/ProfileCard";
import type { CompanyProfileDto } from "@/lib/types";

// ---------------------------------------------------------------------------
// ProfileField tests
// ---------------------------------------------------------------------------

describe("ProfileField", () => {
  it("renders label and value", () => {
    render(<ProfileField label="Nome" value="João da Silva" />);

    expect(screen.getByText("Nome")).toBeInTheDocument();
    expect(screen.getByText("João da Silva")).toBeInTheDocument();
  });

  it("applies read-only styling (no input element)", () => {
    const { container } = render(
      <ProfileField label="Email" value="joao@email.com" />
    );

    // Verify no input element exists (read-only text display)
    expect(container.querySelector("input")).not.toBeInTheDocument();
  });
});

// ---------------------------------------------------------------------------
// ProfileBadge tests — now group-based (admin-empresa, viewer, dashboard)
// ---------------------------------------------------------------------------

describe("ProfileBadge", () => {
  it("shows Admin Empresa for admin-empresa group", () => {
    render(<ProfileBadge group="admin-empresa" />);

    expect(screen.getByText("Admin Empresa")).toBeInTheDocument();
  });

  it("shows Viewer for viewer group", () => {
    render(<ProfileBadge group="viewer" />);

    expect(screen.getByText("Viewer")).toBeInTheDocument();
  });

  it("shows Dashboard for dashboard group", () => {
    render(<ProfileBadge group="dashboard" />);

    expect(screen.getByText("Dashboard")).toBeInTheDocument();
  });

  it("applies green styling to admin-empresa badge", () => {
    const { container } = render(<ProfileBadge group="admin-empresa" />);

    const badge = container.querySelector("span");
    expect(badge?.className).toContain("green");
  });

  it("applies gray styling to viewer badge", () => {
    const { container } = render(<ProfileBadge group="viewer" />);

    const badge = container.querySelector("span");
    expect(badge?.className).toContain("gray");
  });

  it("applies blue styling to dashboard badge", () => {
    const { container } = render(<ProfileBadge group="dashboard" />);

    const badge = container.querySelector("span");
    expect(badge?.className).toContain("blue");
  });
});

// ---------------------------------------------------------------------------
// ProfileCard test data
// ---------------------------------------------------------------------------

const mockCompanyProfile: CompanyProfileDto = {
  id: "test-id-456",
  razaoSocial: "Empresa LTDA",
  cnpj: "12345678000190",
  email: "contato@empresa.com.br",
  phone: "11999990000",
};

// ---------------------------------------------------------------------------
// ProfileCard tests
// ---------------------------------------------------------------------------

describe("ProfileCard", () => {
  it("renders company profile fields", () => {
    render(<ProfileCard profile={mockCompanyProfile} accessGroup="admin-empresa" />);

    expect(screen.getByText("Empresa LTDA")).toBeInTheDocument();
    expect(screen.getByText("12345678000190")).toBeInTheDocument();
    expect(screen.getByText("contato@empresa.com.br")).toBeInTheDocument();
    expect(screen.getByText("11999990000")).toBeInTheDocument();
  });

  it("displays Admin Empresa badge for admin-empresa group", () => {
    render(<ProfileCard profile={mockCompanyProfile} accessGroup="admin-empresa" />);
    expect(screen.getByText("Admin Empresa")).toBeInTheDocument();
  });

  it("displays Viewer badge for viewer group", () => {
    render(<ProfileCard profile={mockCompanyProfile} accessGroup="viewer" />);
    expect(screen.getByText("Viewer")).toBeInTheDocument();
  });
});

// ---------------------------------------------------------------------------
// getProfileClient tests — PJ-only, company endpoint
// ---------------------------------------------------------------------------

describe("getProfileClient — company profile ACF", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("fetches company profile with credentials: include (cookie auth)", async () => {
    const mockProfile: CompanyProfileDto = {
      id: "test-id",
      razaoSocial: "Test Company",
      cnpj: "12345678000190",
      email: "test@company.com",
      phone: "11999990000",
    };

    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockProfile),
    });

    const { getProfileClient } = await import("@/lib/api");
    const result = await getProfileClient();

    expect(result).toEqual(mockProfile);
    expect(global.fetch).toHaveBeenCalledWith("/api/companies/me", {
      method: "GET",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
    });
  });

  it("throws ProfileError on 401 response", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
    });

    const { getProfileClient, ProfileError } = await import("@/lib/api");
    await expect(getProfileClient()).rejects.toThrow(ProfileError);
  });

  it("throws ProfileError on non-401 error response", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
    });

    const { getProfileClient, ProfileError } = await import("@/lib/api");
    await expect(getProfileClient()).rejects.toThrow(ProfileError);
  });
});