import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuditHistorySection } from "@/components/molecules/AuditHistorySection";

// Mock admin-fundos-api
vi.mock("@/lib/admin-fundos-api", () => ({
  getAuditHistory: vi.fn(),
}));

import * as api from "@/lib/admin-fundos-api";

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: 0 } },
  });
  return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
}

const ENTITY_ID = "123e4567-e89b-12d3-a456-426614174000";

describe("AuditHistorySection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders section heading", async () => {
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
    render(
      wrapper({
        children: <AuditHistorySection entityType="Fundo" entityId={ENTITY_ID} />,
      })
    );
    expect(screen.getByRole("region", { name: /histórico/i })).toBeInTheDocument();
  });

  it("shows empty state when no entries", async () => {
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
    render(
      wrapper({
        children: <AuditHistorySection entityType="Fundo" entityId={ENTITY_ID} />,
      })
    );
    await waitFor(() =>
      expect(screen.getByTestId("audit-empty")).toBeInTheDocument()
    );
  });

  it("shows audit entries when returned", async () => {
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [
        {
          id: "entry-1",
          timestamp: "2024-01-15T10:00:00Z",
          adminUserId: "admin-1",
          adminUserName: "admin@test.com",
          actionType: "STATUS_CHANGE",
          targetUserId: null,
          targetUserName: null,
          details: "RASCUNHO -> ATIVO",
          ipAddress: "127.0.0.1",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 10,
    });
    render(
      wrapper({
        children: <AuditHistorySection entityType="Fundo" entityId={ENTITY_ID} />,
      })
    );
    await waitFor(() =>
      expect(screen.getByTestId("audit-entries")).toBeInTheDocument()
    );
    expect(screen.getByTestId("audit-row-entry-1")).toBeInTheDocument();
    expect(screen.getByText("STATUS_CHANGE")).toBeInTheDocument();
  });

  it("shows error state when fetch fails", async () => {
    vi.mocked(api.getAuditHistory).mockRejectedValue(new Error("Network error"));
    render(
      wrapper({
        children: <AuditHistorySection entityType="Fundo" entityId={ENTITY_ID} />,
      })
    );
    await waitFor(() =>
      expect(screen.getByTestId("audit-error")).toBeInTheDocument()
    );
  });

  it("calls getAuditHistory with correct entityType and entityId", async () => {
    vi.mocked(api.getAuditHistory).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    });
    render(
      wrapper({
        children: <AuditHistorySection entityType="Cedente" entityId={ENTITY_ID} />,
      })
    );
    await waitFor(() => {
      expect(api.getAuditHistory).toHaveBeenCalledWith(
        expect.objectContaining({ entityType: "Cedente", entityId: ENTITY_ID })
      );
    });
  });
});
