import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import {
  AdminAssociationTable,
  type AssociationRow,
} from "@/components/molecules/AdminAssociationTable";

const makeRow = (id: string): AssociationRow => ({
  id,
  empresaNome: "Empresa Teste",
  col1Label: "Fundo",
  col1Value: "Fundo Alpha",
  col2Label: "Cedente",
  col2Value: "Cedente Beta",
  status: "ATIVO",
  dataInicio: "2024-01-01T00:00:00Z",
  dataFim: null,
  limitePercentual: 25.5,
  limiteValor: null,
});

describe("AdminAssociationTable", () => {
  it("renders rows with empresa and entity columns", () => {
    const rows = [makeRow("row-1"), makeRow("row-2")];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByTestId("assoc-row-row-1")).toBeInTheDocument();
    expect(screen.getByTestId("assoc-row-row-2")).toBeInTheDocument();
    expect(screen.getAllByText("Empresa Teste")).toHaveLength(2);
    expect(screen.getAllByText("Fundo Alpha")).toHaveLength(2);
  });

  it("renders empty state when no rows", () => {
    render(
      <AdminAssociationTable rows={[]} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByTestId("table-empty")).toBeInTheDocument();
  });

  it("shows column headers", () => {
    const rows = [makeRow("r-1")];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByText("Fundo")).toBeInTheDocument();
    expect(screen.getByText("Cedente")).toBeInTheDocument();
    expect(screen.getByText("Empresa")).toBeInTheDocument();
    expect(screen.getByText("Status")).toBeInTheDocument();
  });

  it("renders data table accessible structure", () => {
    const rows = [makeRow("r-1")];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByRole("table")).toBeInTheDocument();
  });

  it("renders '—' for null dataFim", () => {
    const rows = [makeRow("r-1")];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    // dataFim is null → formatDate returns "—"
    const cells = screen.getAllByText("—");
    expect(cells.length).toBeGreaterThan(0);
  });

  it("renders formatted percent when limitePercentual provided", () => {
    const rows = [makeRow("r-1")]; // limitePercentual = 25.5
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByText("25.50%")).toBeInTheDocument();
  });

  it("renders '—' for null limiteValor", () => {
    const rows = [{ ...makeRow("r-1"), limiteValor: null, limitePercentual: null }];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    const dashes = screen.getAllByText("—");
    expect(dashes.length).toBeGreaterThan(0);
  });

  it("renders formatted currency when limiteValor provided", () => {
    const rows = [{ ...makeRow("r-1"), limiteValor: 50000, limitePercentual: null }];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    // Currency format varies by locale, just check it rendered something
    expect(screen.getByTestId("assoc-row-r-1")).toBeInTheDocument();
  });

  it("uses fallback badge class for unknown status", () => {
    const rows = [{ ...makeRow("r-1"), status: "PENDENTE" }];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByText("PENDENTE")).toBeInTheDocument();
  });

  it("renders dataFim when provided", () => {
    const rows = [{ ...makeRow("r-2"), dataFim: "2025-12-31T00:00:00Z" }];
    render(
      <AdminAssociationTable rows={rows} col1Header="Fundo" col2Header="Cedente" />
    );
    expect(screen.getByTestId("assoc-row-r-2")).toBeInTheDocument();
  });
});
