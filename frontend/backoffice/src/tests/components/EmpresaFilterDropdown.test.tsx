import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { EmpresaFilterDropdown } from "@/components/molecules/EmpresaFilterDropdown";

// Mock admin-companies-api
vi.mock("@/lib/admin-companies-api", () => ({
  listCompaniesForFilter: vi.fn().mockResolvedValue([
    { id: "empresa-1", razaoSocial: "Empresa Alpha" },
    { id: "empresa-2", razaoSocial: "Empresa Beta" },
  ]),
}));

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: 0 } },
  });
  return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
}

describe("EmpresaFilterDropdown", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders dropdown with 'Todas as empresas' option by default", async () => {
    render(
      wrapper({
        children: <EmpresaFilterDropdown value={undefined} onChange={vi.fn()} />,
      })
    );
    await waitFor(() =>
      expect(screen.getByRole("combobox")).toBeInTheDocument()
    );
    expect(screen.getByText("Todas as empresas")).toBeInTheDocument();
  });

  it("loads and shows company options", async () => {
    render(
      wrapper({
        children: <EmpresaFilterDropdown value={undefined} onChange={vi.fn()} />,
      })
    );
    await waitFor(() => {
      expect(screen.getByText("Empresa Alpha")).toBeInTheDocument();
      expect(screen.getByText("Empresa Beta")).toBeInTheDocument();
    });
  });

  it("calls onChange with empresa id when selected", async () => {
    const onChange = vi.fn();
    render(
      wrapper({
        children: <EmpresaFilterDropdown value={undefined} onChange={onChange} />,
      })
    );
    await waitFor(() => {
      expect(screen.getByText("Empresa Alpha")).toBeInTheDocument();
    });
    fireEvent.change(screen.getByRole("combobox"), { target: { value: "empresa-1" } });
    expect(onChange).toHaveBeenCalledWith("empresa-1");
  });

  it("calls onChange with undefined when 'Todas as empresas' selected", async () => {
    const onChange = vi.fn();
    render(
      wrapper({
        children: <EmpresaFilterDropdown value="empresa-1" onChange={onChange} />,
      })
    );
    await waitFor(() =>
      expect(screen.getByText("Empresa Alpha")).toBeInTheDocument()
    );
    fireEvent.change(screen.getByRole("combobox"), { target: { value: "" } });
    expect(onChange).toHaveBeenCalledWith(undefined);
  });

  it("has accessible label via aria-label", async () => {
    render(
      wrapper({
        children: <EmpresaFilterDropdown value={undefined} onChange={vi.fn()} />,
      })
    );
    await waitFor(() =>
      expect(screen.getByRole("combobox")).toBeInTheDocument()
    );
    expect(screen.getByRole("combobox")).toHaveAttribute("aria-label");
  });
});
