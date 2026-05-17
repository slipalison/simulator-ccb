// ---------------------------------------------------------------------------
// ConsultoriaFundoTable.test.tsx — render + interaction (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { ConsultoriaFundoTable } from "@/components/organisms/ConsultoriaFundoTable";
import type { ConsultoriaFundoDto } from "@/lib/fundos-schemas";

const SAMPLE: ConsultoriaFundoDto = {
  id: "uuid-cons-1",
  razaoSocial: "Consultoria SA",
  nomeFantasia: null,
  cnpj: "11222333000181",
  email: null,
  telefone: null,
  status: "ATIVO",
  createdAt: "2020-01-01T00:00:00Z",
};

describe("ConsultoriaFundoTable", () => {
  it("shows skeleton when loading", () => {
    const { container } = render(
      <ConsultoriaFundoTable items={[]} isLoading={true} canWrite={false} onEdit={vi.fn()} />
    );
    expect(container.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it("shows empty state message", () => {
    render(
      <ConsultoriaFundoTable items={[]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.getByText(/nenhuma consultoria encontrada/i)).toBeInTheDocument();
  });

  it("renders consultoria row", () => {
    render(
      <ConsultoriaFundoTable items={[SAMPLE]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.getByText("Consultoria SA")).toBeInTheDocument();
    expect(screen.getByText("11222333000181")).toBeInTheDocument();
  });

  it("calls onEdit when edit button clicked (canWrite=true)", () => {
    const onEdit = vi.fn();
    render(
      <ConsultoriaFundoTable items={[SAMPLE]} isLoading={false} canWrite={true} onEdit={onEdit} />
    );
    fireEvent.click(screen.getByRole("button", { name: /editar consultoria/i }));
    expect(onEdit).toHaveBeenCalledWith(SAMPLE);
  });

  it("hides edit button when canWrite=false", () => {
    render(
      <ConsultoriaFundoTable items={[SAMPLE]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.queryByRole("button", { name: /editar/i })).not.toBeInTheDocument();
  });
});
