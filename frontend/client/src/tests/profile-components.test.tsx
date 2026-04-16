// ---------------------------------------------------------------------------
// GREEN tests — Profile components and API client
// ---------------------------------------------------------------------------
// Updated in plan 33-01: getProfileClient now uses cookies (no Bearer token).
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { ProfileField } from "@/components/atoms/ProfileField";
import { ProfileBadge } from "@/components/atoms/ProfileBadge";
import { ProfileCard } from "@/components/molecules/ProfileCard";
import type { ClientProfileDto } from "@/lib/types";

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
// ProfileBadge tests
// ---------------------------------------------------------------------------

describe("ProfileBadge", () => {
  it("shows Pessoa Física for PF type", () => {
    render(<ProfileBadge type="PessoaFisica" />);

    expect(screen.getByText("Pessoa Física")).toBeInTheDocument();
  });

  it("shows Pessoa Jurídica for PJ type", () => {
    render(<ProfileBadge type="PessoaJuridica" />);

    expect(screen.getByText("Pessoa Jurídica")).toBeInTheDocument();
  });

  it("applies green styling to PF badge", () => {
    const { container } = render(<ProfileBadge type="PessoaFisica" />);

    // Implementation uses bg-green-100 (Tailwind) — contains "green"
    const badge = container.querySelector("span");
    expect(badge?.className).toContain("green");
  });

  it("applies blue styling to PJ badge", () => {
    const { container } = render(<ProfileBadge type="PessoaJuridica" />);

    // Implementation uses bg-blue-100 (Tailwind) — contains "blue"
    const badge = container.querySelector("span");
    expect(badge?.className).toContain("blue");
  });
});

// ---------------------------------------------------------------------------
// ProfileCard test data
// ---------------------------------------------------------------------------

const mockPFProfile: ClientProfileDto = {
  id: "test-id-123",
  name: "João da Silva",
  email: "joao@email.com",
  phone: "(11) 99999-9999",
  type: "PessoaFisica",
  cpf: "123.456.789-00",
  cnpj: null,
  razaoSocial: null,
};

const mockPJProfile: ClientProfileDto = {
  id: "test-id-456",
  name: "Empresa LTDA",
  email: "contato@empresa.com.br",
  phone: "(11) 3333-4444",
  type: "PessoaJuridica",
  cpf: null,
  cnpj: "12.345.678/0001-90",
  razaoSocial: "Empresa LTDA",
};

// ---------------------------------------------------------------------------
// ProfileCard tests
// ---------------------------------------------------------------------------

describe("ProfileCard", () => {
  it("renders PF profile fields", () => {
    render(<ProfileCard profile={mockPFProfile} />);

    expect(screen.getByText("João da Silva")).toBeInTheDocument();
    expect(screen.getByText("123.456.789-00")).toBeInTheDocument();
    expect(screen.getByText("joao@email.com")).toBeInTheDocument();
    expect(screen.getByText("(11) 99999-9999")).toBeInTheDocument();
  });

  it("renders PJ profile fields", () => {
    render(<ProfileCard profile={mockPJProfile} />);

    expect(screen.getByText("Empresa LTDA")).toBeInTheDocument();
    expect(screen.getByText("12.345.678/0001-90")).toBeInTheDocument();
    expect(screen.getByText("contato@empresa.com.br")).toBeInTheDocument();
    expect(screen.getByText("(11) 3333-4444")).toBeInTheDocument();
  });

  it("does not show CPF field label for PJ profiles", () => {
    render(<ProfileCard profile={mockPJProfile} />);

    // PJ profile should not display the CPF label
    expect(screen.queryByText("CPF")).not.toBeInTheDocument();
  });

  it("does not show CNPJ field label for PF profiles", () => {
    render(<ProfileCard profile={mockPFProfile} />);

    // PF profile should not display the CNPJ label
    expect(screen.queryByText("CNPJ")).not.toBeInTheDocument();
  });

  it("displays ProfileBadge with correct type", () => {
    const { rerender } = render(<ProfileCard profile={mockPFProfile} />);
    expect(screen.getByText("Pessoa Física")).toBeInTheDocument();

    rerender(<ProfileCard profile={mockPJProfile} />);
    expect(screen.getByText("Pessoa Jurídica")).toBeInTheDocument();
  });
});

// ---------------------------------------------------------------------------
// getProfileClient tests — cookie-based ACF version
// ---------------------------------------------------------------------------
// getProfileClient() uses credentials: "include" (httpOnly cookie auth).
// No Bearer token needed — auth is handled by Vinxi auth-server via cookies.
// ---------------------------------------------------------------------------

describe("getProfileClient — cookie-based ACF", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("fetches profile with credentials: include (cookie auth)", async () => {
    const mockProfile: ClientProfileDto = {
      id: "test-id",
      name: "Test User",
      email: "test@email.com",
      phone: "(11) 99999-9999",
      type: "PessoaFisica",
      cpf: "123.456.789-00",
      cnpj: null,
      razaoSocial: null,
    };

    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockProfile),
    });

    const { getProfileClient } = await import("@/lib/api");
    const result = await getProfileClient();

    expect(result).toEqual(mockProfile);
    expect(global.fetch).toHaveBeenCalledWith("/api/clients/me", {
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
