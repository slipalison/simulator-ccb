// ---------------------------------------------------------------------------
// AuditEventRow — render, timestamp, entity fields
// ---------------------------------------------------------------------------

import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { AuditEventRow } from "@/components/atoms/AuditEventRow";
import type { AuditLogEntry } from "@/lib/admin-fundos-schemas";

const BASE_ENTRY: AuditLogEntry = {
  id: "entry-1",
  timestamp: "2024-06-15T10:30:00Z",
  adminUserId: "admin-1",
  adminUserName: "admin@test.com",
  actionType: "STATUS_CHANGE",
  targetUserId: null,
  targetUserName: null,
  details: "RASCUNHO -> ATIVO",
  ipAddress: "127.0.0.1",
  entityType: null,
  entityId: null,
};

describe("AuditEventRow", () => {
  it("renders action type", () => {
    render(<AuditEventRow entry={BASE_ENTRY} />);
    expect(screen.getByText("STATUS_CHANGE")).toBeInTheDocument();
  });

  it("renders admin user name", () => {
    render(<AuditEventRow entry={BASE_ENTRY} />);
    expect(screen.getByText("admin@test.com")).toBeInTheDocument();
  });

  it("renders details when provided", () => {
    render(<AuditEventRow entry={BASE_ENTRY} />);
    expect(screen.getByText("RASCUNHO -> ATIVO")).toBeInTheDocument();
  });

  it("does not render details section when details is null", () => {
    render(<AuditEventRow entry={{ ...BASE_ENTRY, details: null }} />);
    expect(screen.queryByText("RASCUNHO -> ATIVO")).not.toBeInTheDocument();
  });

  it("renders targetUserName when provided", () => {
    render(<AuditEventRow entry={{ ...BASE_ENTRY, targetUserName: "user@example.com" }} />);
    expect(screen.getByText(/user@example\.com/)).toBeInTheDocument();
  });

  it("renders with data-testid for the row", () => {
    render(<AuditEventRow entry={BASE_ENTRY} />);
    expect(screen.getByTestId("audit-row-entry-1")).toBeInTheDocument();
  });

  it("renders formatted timestamp using locale", () => {
    render(<AuditEventRow entry={BASE_ENTRY} />);
    const time = screen.getByRole("time");
    expect(time).toBeInTheDocument();
    expect(time).toHaveAttribute("dateTime", "2024-06-15T10:30:00Z");
  });

  it("does not throw on invalid date timestamp", () => {
    // new Date("not-a-date").toLocaleString() returns "Invalid Date" in most envs; component must not crash
    expect(() =>
      render(<AuditEventRow entry={{ ...BASE_ENTRY, timestamp: "not-a-date" }} />)
    ).not.toThrow();
    // time element is still rendered
    expect(screen.getByRole("time")).toBeInTheDocument();
  });

  it("renders entityType and entityId caption when provided", () => {
    render(
      <AuditEventRow
        entry={{
          ...BASE_ENTRY,
          entityType: "Fundo",
          entityId: "123e4567-e89b-12d3-a456-426614174000",
        }}
      />
    );
    expect(screen.getByTestId("audit-entity-caption")).toBeInTheDocument();
    expect(screen.getByTestId("audit-entity-caption")).toHaveTextContent("Fundo");
    expect(screen.getByTestId("audit-entity-caption")).toHaveTextContent("123e4567-e89b-12d3-a456-426614174000");
  });

  it("does not render entity caption when entityType is null", () => {
    render(<AuditEventRow entry={BASE_ENTRY} />);
    expect(screen.queryByTestId("audit-entity-caption")).not.toBeInTheDocument();
  });
});
