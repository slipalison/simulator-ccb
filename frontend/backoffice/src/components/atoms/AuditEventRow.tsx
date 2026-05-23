// ---------------------------------------------------------------------------
// AuditEventRow — single audit log event display (T-7)
// ---------------------------------------------------------------------------

import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import type { AuditLogEntry } from "@/lib/admin-fundos-schemas";

interface AuditEventRowProps {
  entry: AuditLogEntry;
}

export function AuditEventRow({ entry }: AuditEventRowProps) {
  const timestamp = (() => {
    try {
      return new Date(entry.timestamp).toLocaleString("pt-BR");
    } catch {
      return entry.timestamp;
    }
  })();

  return (
    <div
      className="flex flex-col sm:flex-row sm:items-start gap-1 sm:gap-4 py-3 border-b last:border-0 text-sm"
      data-testid={`audit-row-${entry.id}`}
    >
      <time
        dateTime={entry.timestamp}
        className="text-muted-foreground shrink-0 w-40"
        aria-label={`${L.auditTimestamp}: ${timestamp}`}
      >
        {timestamp}
      </time>
      <div className="flex-1 min-w-0">
        <span className="font-medium">{entry.actionType}</span>
        {entry.targetUserName && (
          <span className="text-muted-foreground ml-2">→ {entry.targetUserName}</span>
        )}
        {entry.details && (
          <p className="text-muted-foreground text-xs mt-0.5 truncate">{entry.details}</p>
        )}
      </div>
      <span className="text-muted-foreground shrink-0 text-xs">
        {entry.adminUserName}
      </span>
    </div>
  );
}
