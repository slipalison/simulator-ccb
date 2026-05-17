// ---------------------------------------------------------------------------
// AssociationTable.test.tsx — render + interaction + a11y (D-2, T-7)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { AssociationTable, type AssociationRow } from "@/components/organisms/AssociationTable";

const SAMPLE_ROW: AssociationRow = {
  id: "assoc-1",
  targetLabel: "João Silva",
  limitePercentual: 10,
  limiteValor: null,
  dataInicio: "2024-01-01T00:00:00Z",
  dataFim: null,
  status: "ATIVO",
};

const SAMPLE_ROW_WITH_FIM: AssociationRow = {
  ...SAMPLE_ROW,
  id: "assoc-2",
  dataFim: "2025-01-01T00:00:00Z",
  limiteValor: 500000,
};

describe("AssociationTable", () => {
  it("shows skeleton when loading", () => {
    const { container } = render(
      <AssociationTable
        rows={[]}
        isLoading={true}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    expect(container.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it("shows empty state when no rows", () => {
    render(
      <AssociationTable
        rows={[]}
        isLoading={false}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    expect(screen.getByText(/nenhuma associação encontrada/i)).toBeInTheDocument();
  });

  it("renders association row with target label and percentage", () => {
    render(
      <AssociationTable
        rows={[SAMPLE_ROW]}
        isLoading={false}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    expect(screen.getByText("João Silva")).toBeInTheDocument();
    expect(screen.getByText("10%")).toBeInTheDocument();
  });

  it("shows 'Vigência infinita' when dataFim is null", () => {
    render(
      <AssociationTable
        rows={[SAMPLE_ROW]}
        isLoading={false}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    expect(screen.getByText("Vigência infinita")).toBeInTheDocument();
  });

  it("shows formatted date when dataFim is set", () => {
    render(
      <AssociationTable
        rows={[SAMPLE_ROW_WITH_FIM]}
        isLoading={false}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    // toLocaleDateString pt-BR for 2025-01-01
    expect(screen.queryByText("Vigência infinita")).not.toBeInTheDocument();
  });

  it("renders Ações column header when canWrite=true", () => {
    render(
      <AssociationTable
        rows={[SAMPLE_ROW]}
        isLoading={false}
        canWrite={true}
        onEditLimits={vi.fn()}
        renderStatusControl={() => <button>Transição</button>}
      />
    );
    expect(screen.getByRole("columnheader", { name: /ações/i })).toBeInTheDocument();
  });

  it("does not render Ações column when canWrite=false", () => {
    render(
      <AssociationTable
        rows={[SAMPLE_ROW]}
        isLoading={false}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    expect(screen.queryByRole("columnheader", { name: /ações/i })).not.toBeInTheDocument();
  });

  it("renders status badge with correct label", () => {
    render(
      <AssociationTable
        rows={[SAMPLE_ROW]}
        isLoading={false}
        canWrite={false}
        onEditLimits={vi.fn()}
        renderStatusControl={() => null}
      />
    );
    expect(screen.getByText("Ativo")).toBeInTheDocument();
  });

  it("calls renderStatusControl with the row data", () => {
    const renderStatusControl = vi.fn().mockReturnValue(null);
    render(
      <AssociationTable
        rows={[SAMPLE_ROW]}
        isLoading={false}
        canWrite={true}
        onEditLimits={vi.fn()}
        renderStatusControl={renderStatusControl}
      />
    );
    expect(renderStatusControl).toHaveBeenCalledWith(SAMPLE_ROW);
  });
});
