// ---------------------------------------------------------------------------
// StatusTransitionDropdown: dropdown for state machine transitions (D-25)
// Fetches allowed transitions from backend (source of truth).
// Shared by Fundo and 3 association aggregates.
// ---------------------------------------------------------------------------

import { useState } from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { ChevronDown, Loader2 } from "lucide-react";

interface StatusTransitionDropdownProps<TStatus extends string> {
  currentStatus: TStatus;
  allowedTransitions: TStatus[] | undefined;
  isLoadingTransitions: boolean;
  onTransition: (newStatus: TStatus) => Promise<void>;
  statusLabels: Record<string, string>;
  disabled?: boolean;
  label?: string;
}

/**
 * Dropdown for transitioning an entity's status.
 * Only shows valid next states from backend allowed-transitions endpoint (D-25).
 * Calls onTransition when user selects a new status.
 */
export function StatusTransitionDropdown<TStatus extends string>({
  currentStatus,
  allowedTransitions,
  isLoadingTransitions,
  onTransition,
  statusLabels,
  disabled = false,
  label = "Alterar status",
}: StatusTransitionDropdownProps<TStatus>) {
  const [isPending, setIsPending] = useState(false);

  const hasTransitions = allowedTransitions && allowedTransitions.length > 0;
  const isDisabled = disabled || isPending || isLoadingTransitions || !hasTransitions;

  async function handleSelect(newStatus: TStatus) {
    setIsPending(true);
    try {
      await onTransition(newStatus);
    } finally {
      setIsPending(false);
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          disabled={isDisabled}
          aria-label={label}
          aria-haspopup="listbox"
        >
          {isPending || isLoadingTransitions ? (
            <Loader2 className="h-4 w-4 animate-spin mr-2" aria-hidden="true" />
          ) : null}
          {statusLabels[currentStatus] ?? currentStatus}
          <ChevronDown className="ml-2 h-4 w-4" aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" role="listbox" aria-label="Transições de status disponíveis">
        {(allowedTransitions ?? []).map((status) => (
          <DropdownMenuItem
            key={status}
            role="option"
            aria-selected={false}
            onSelect={() => void handleSelect(status)}
          >
            {statusLabels[status] ?? status}
          </DropdownMenuItem>
        ))}
        {!hasTransitions && !isLoadingTransitions && (
          <DropdownMenuItem disabled>
            Nenhuma transição disponível
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
