// ---------------------------------------------------------------------------
// FundoStatusBadge: visual status badge for Fundo (Phase 50 color spec)
// ---------------------------------------------------------------------------

import { cn } from "@/lib/utils";
import type { FundoStatus } from "@/lib/fundos-schemas";
import { FUNDO_STATUS_COLORS, FUNDO_STATUS_LABELS } from "@/lib/fundos-schemas";

interface FundoStatusBadgeProps {
  status: FundoStatus;
  className?: string;
}

/**
 * Color-coded status badge for Fundo.
 * Colors per Phase 50 spec: RASCUNHO=gray, ATIVO=green, SUSPENSO=yellow,
 * EM_LIQUIDACAO=orange, ENCERRADO=red.
 */
export function FundoStatusBadge({ status, className }: FundoStatusBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold text-white",
        FUNDO_STATUS_COLORS[status],
        className
      )}
      aria-label={`Status: ${FUNDO_STATUS_LABELS[status]}`}
    >
      {FUNDO_STATUS_LABELS[status]}
    </span>
  );
}
