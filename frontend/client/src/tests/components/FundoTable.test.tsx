// ---------------------------------------------------------------------------
// FundoTable.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { FundoTable } from "@/components/organisms/FundoTable";
import type { FundoDto } from "@/lib/fundos-schemas";

const SAMPLE_FUNDO: FundoDto = {
  id: "uuid-fundo-1",
  nome: "Fundo Alpha",
  cnpj: "11222333000181",
  consultoriaFundoId: "uuid-cons-1",
  custodianteId: "uuid-cust-1",
  tipoFundo: "Multimercado",
  classeAnbima: "Macro",
  segmento: null,
  dataConstituicao: "2020-01-01T00:00:00Z",
  status: "ATIVO",
  createdAt: "2020-01-01T00:00:00Z",
};

describe("FundoTable", () => {
  it("shows skeleton when loading", () => {
    const { container } = render(
      <FundoTable items={[]} isLoading={true} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(container.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it("shows empty state message when no items", () => {
    render(
      <FundoTable items={[]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByText(/nenhum fundo encontrado/i)).toBeInTheDocument();
  });

  it("renders fundo rows with name and cnpj", () => {
    render(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByText("Fundo Alpha")).toBeInTheDocument();
    expect(screen.getByText("11222333000181")).toBeInTheDocument();
  });

  it("renders FundoStatusBadge with correct status", () => {
    render(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByLabelText(/status: ativo/i)).toBeInTheDocument();
  });

  it("calls onDetail when clicking fund name", () => {
    const onDetail = vi.fn();
    render(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={onDetail} />
    );
    // There are two detail buttons per row (name button + eye icon button)
    const detailButtons = screen.getAllByRole("button", { name: /ver detalhes do fundo fundo alpha/i });
    fireEvent.click(detailButtons[0]);
    expect(onDetail).toHaveBeenCalledWith(SAMPLE_FUNDO);
  });

  it("shows edit button only when canWrite is true", () => {
    const { rerender } = render(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.queryByRole("button", { name: /editar fundo fundo alpha/i })).not.toBeInTheDocument();

    rerender(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={true} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByRole("button", { name: /editar fundo fundo alpha/i })).toBeInTheDocument();
  });

  it("calls onEdit when clicking edit button", () => {
    const onEdit = vi.fn();
    render(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={true} onEdit={onEdit} onDetail={vi.fn()} />
    );
    fireEvent.click(screen.getByRole("button", { name: /editar fundo fundo alpha/i }));
    expect(onEdit).toHaveBeenCalledWith(SAMPLE_FUNDO);
  });

  it("displays tipo fundo label", () => {
    render(
      <FundoTable items={[SAMPLE_FUNDO]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByText("Multimercado")).toBeInTheDocument();
  });
});
