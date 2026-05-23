// ---------------------------------------------------------------------------
// AuditHistorySection — inline audit history for entity detail pages (T-7, D-30)
// Consumes GET /api/admin/audit-log?entityType=X&entityId=Y via TanStack Query.
// T-1 backend adds entityType+entityId filter support.
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { getAuditHistory } from "@/lib/admin-fundos-api";
import { AuditEventRow } from "@/components/atoms/AuditEventRow";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

interface AuditHistorySectionProps {
  entityType: string;
  entityId: string;
  pageSize?: number;
}

export function AuditHistorySection({
  entityType,
  entityId,
  pageSize = 10,
}: AuditHistorySectionProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-audit-history", entityType, entityId],
    queryFn: () => getAuditHistory({ entityType, entityId, page: 1, pageSize }),
    enabled: !!entityId,
  });

  const entries = data?.items ?? [];

  return (
    <section
      aria-labelledby="audit-history-heading"
      className="mt-8"
      data-testid="audit-history-section"
    >
      <h2
        id="audit-history-heading"
        className="text-base font-semibold mb-3"
      >
        {L.detailSectionHistorico}
      </h2>

      {isLoading && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground py-4">
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
          <span>{L.auditLoadingHistory}</span>
        </div>
      )}

      {isError && !isLoading && (
        <p className="text-sm text-destructive py-4" data-testid="audit-error">
          {L.auditErrorHistory}
        </p>
      )}

      {!isLoading && !isError && entries.length === 0 && (
        <p className="text-sm text-muted-foreground py-4" data-testid="audit-empty">
          {L.auditNoHistory}
        </p>
      )}

      {!isLoading && !isError && entries.length > 0 && (
        <div data-testid="audit-entries">
          {entries.map((entry) => (
            <AuditEventRow key={entry.id} entry={entry} />
          ))}
        </div>
      )}
    </section>
  );
}
