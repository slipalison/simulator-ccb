// ---------------------------------------------------------------------------
// TipoAtivoTable.test.tsx — render + interaction (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { TipoAtivoTable } from "@/components/organisms/TipoAtivoTable";
import type { TipoAtivoDto } from "@/lib/fundos-schemas";

const SAMPLE: TipoAtivoDto = {
  id: "uuid-tipo-1",
  codigo: "RF001",
  descricao: "CDB Renda Fixa",
  categoria: "RendaFixa",
  subcategoria: null,
  status: "ATIVO",
  ordemExibicao: 1,
};

describe("TipoAtivoTable", () => {
  it("shows skeleton when loading", () => {
    const { container } = render(
      <TipoAtivoTable items={[]} isLoading={true} canWrite={false} onEdit={vi.fn()} />
    );
    expect(container.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it("shows empty state message", () => {
    render(
      <TipoAtivoTable items={[]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.getByText(/nenhum tipo de ativo encontrado/i)).toBeInTheDocument();
  });

  it("renders tipo de ativo row with codigo and descricao", () => {
    render(
      <TipoAtivoTable items={[SAMPLE]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.getByText("RF001")).toBeInTheDocument();
    expect(screen.getByText("CDB Renda Fixa")).toBeInTheDocument();
  });

  it("shows edit button when canWrite=true and calls onEdit", () => {
    const onEdit = vi.fn();
    render(
      <TipoAtivoTable items={[SAMPLE]} isLoading={false} canWrite={true} onEdit={onEdit} />
    );
    fireEvent.click(screen.getByRole("button", { name: /editar tipo de ativo/i }));
    expect(onEdit).toHaveBeenCalledWith(SAMPLE);
  });

  it("hides edit button when canWrite=false", () => {
    render(
      <TipoAtivoTable items={[SAMPLE]} isLoading={false} canWrite={false} onEdit={vi.fn()} />
    );
    expect(screen.queryByRole("button", { name: /editar/i })).not.toBeInTheDocument();
  });
});
