import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { AdminFieldsGrid } from "@/components/molecules/AdminFieldsGrid";

describe("AdminFieldsGrid", () => {
  const fields = [
    { label: "ID", value: "abc-123", testId: "field-id" },
    { label: "Nome", value: "Fundo Teste", testId: "field-nome" },
    { label: "Vazio", value: null, testId: "field-vazio" },
    { label: "Vazio String", value: "", testId: "field-empty-str" },
  ];

  it("renders all fields", () => {
    render(<AdminFieldsGrid fields={fields} />);
    expect(screen.getByTestId("fields-grid")).toBeInTheDocument();
    expect(screen.getByText("ID")).toBeInTheDocument();
    expect(screen.getByText("Nome")).toBeInTheDocument();
  });

  it("renders field value", () => {
    render(<AdminFieldsGrid fields={fields} />);
    expect(screen.getByTestId("field-id")).toHaveTextContent("abc-123");
    expect(screen.getByTestId("field-nome")).toHaveTextContent("Fundo Teste");
  });

  it("renders em-dash for null value", () => {
    render(<AdminFieldsGrid fields={fields} />);
    expect(screen.getByTestId("field-vazio")).toHaveTextContent("—");
  });

  it("renders em-dash for empty string value", () => {
    render(<AdminFieldsGrid fields={fields} />);
    expect(screen.getByTestId("field-empty-str")).toHaveTextContent("—");
  });

  it("uses dl/dt/dd semantic structure", () => {
    render(<AdminFieldsGrid fields={fields} />);
    // dl > div > dt structure — dt has role "term" but is unnamed (content not used for a11y name)
    // Verify by checking there are dt elements present
    const dl = screen.getByTestId("fields-grid");
    const terms = dl.querySelectorAll("dt");
    expect(terms.length).toBe(4);
    expect(terms[0].textContent).toBe("ID");
  });
});
