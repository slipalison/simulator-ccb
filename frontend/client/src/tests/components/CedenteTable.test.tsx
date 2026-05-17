// ---------------------------------------------------------------------------
// CedenteTable.test.tsx — render + interaction + a11y (D-2)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { CedenteTable } from "@/components/organisms/CedenteTable";
import type { CedenteDto } from "@/lib/fundos-schemas";

const SAMPLE_CEDENTE: CedenteDto = {
  id: "uuid-ced-1",
  documento: "12345678901",
  nome: "João Silva",
  email: "joao@example.com",
  telefone: null,
  endereco: null,
  cedenteTipo: "PF",
  status: "ATIVO",
  createdAt: "2020-01-01T00:00:00Z",
};

describe("CedenteTable", () => {
  it("shows skeleton when loading", () => {
    const { container } = render(
      <CedenteTable items={[]} isLoading={true} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(container.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it("shows empty state when no items", () => {
    render(
      <CedenteTable items={[]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByText(/nenhum cedente encontrado/i)).toBeInTheDocument();
  });

  it("renders cedente row with name and document", () => {
    render(
      <CedenteTable items={[SAMPLE_CEDENTE]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByText("João Silva")).toBeInTheDocument();
    expect(screen.getByText("12345678901")).toBeInTheDocument();
  });

  it("calls onDetail when name button clicked", () => {
    const onDetail = vi.fn();
    render(
      <CedenteTable items={[SAMPLE_CEDENTE]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={onDetail} />
    );
    fireEvent.click(screen.getByRole("button", { name: /ver detalhes.*joão silva/i }));
    expect(onDetail).toHaveBeenCalledWith(SAMPLE_CEDENTE);
  });

  it("shows edit button only when canWrite", () => {
    const { rerender } = render(
      <CedenteTable items={[SAMPLE_CEDENTE]} isLoading={false} canWrite={false} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.queryByRole("button", { name: /editar cedente joão silva/i })).not.toBeInTheDocument();

    rerender(
      <CedenteTable items={[SAMPLE_CEDENTE]} isLoading={false} canWrite={true} onEdit={vi.fn()} onDetail={vi.fn()} />
    );
    expect(screen.getByRole("button", { name: /editar cedente joão silva/i })).toBeInTheDocument();
  });
});
