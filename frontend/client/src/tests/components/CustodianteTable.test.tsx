// ---------------------------------------------------------------------------
// CustodianteTable.test.tsx — render + interaction (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { CustodianteTable } from "@/components/organisms/CustodianteTable";
import type { CustodianteDto } from "@/lib/fundos-schemas";

const SAMPLE: CustodianteDto = {
  id: "uuid-cust-1",
  razaoSocial: "Custodiante SA",
  codigoInterno: "CUST-01",
  cnpj: "11222333000181",
  email: null,
  telefone: null,
  status: "ATIVO",
  createdAt: "2020-01-01T00:00:00Z",
};

describe("CustodianteTable", () => {
  it("shows skeleton when loading", () => {
    const { container } = render(
      <CustodianteTable items={[]} isLoading={true} canWrite={false} onEdit={vi.fn()} />
    );
    expect(container.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it("shows empty state message", () => {
    render(
      <CustodianteTable items={[]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.getByText(/nenhum custodiante encontrado/i)).toBeInTheDocument();
  });

  it("renders custodiante row with razão social and cnpj", () => {
    render(
      <CustodianteTable items={[SAMPLE]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.getByText("Custodiante SA")).toBeInTheDocument();
    expect(screen.getByText("11222333000181")).toBeInTheDocument();
  });

  it("shows edit button when canWrite=true and calls onEdit", () => {
    const onEdit = vi.fn();
    render(
      <CustodianteTable items={[SAMPLE]} isLoading={false} canWrite={true} onEdit={onEdit} />
    );
    fireEvent.click(screen.getByRole("button", { name: /editar custodiante/i }));
    expect(onEdit).toHaveBeenCalledWith(SAMPLE);
  });

  it("hides edit button when canWrite=false", () => {
    render(
      <CustodianteTable items={[SAMPLE]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.queryByRole("button", { name: /editar/i })).not.toBeInTheDocument();
  });
});
