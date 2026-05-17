// ---------------------------------------------------------------------------
// useAllowedTransitions hook (D-25)
// Fetches allowed next states from backend GET .../allowed-transitions.
// Returns string[] (FundoStatus[] or RelationshipStatus[]).
// Backend is the single source of truth — frontend uses result for UI only.
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import {
  getFundoAllowedTransitions,
  getFundoCedenteAllowedTransitions,
  getFundoTipoAtivoAllowedTransitions,
  getCedenteTipoAtivoAllowedTransitions,
} from "@/lib/fundos-api";
import type { FundoStatus, RelationshipStatus } from "@/lib/fundos-schemas";

// ---------------------------------------------------------------------------
// useFundoAllowedTransitions
// ---------------------------------------------------------------------------

export function useFundoAllowedTransitions(fundoId: string | undefined) {
  return useQuery<FundoStatus[]>({
    queryKey: ["allowed-transitions", "fundo", fundoId],
    queryFn: () => getFundoAllowedTransitions(fundoId!),
    enabled: !!fundoId,
    staleTime: 5_000, // transitions change infrequently; 5s freshness
  });
}

// ---------------------------------------------------------------------------
// useFundoCedenteAllowedTransitions
// ---------------------------------------------------------------------------

export function useFundoCedenteAllowedTransitions(
  fundoId: string | undefined,
  associationId: string | undefined
) {
  return useQuery<RelationshipStatus[]>({
    queryKey: ["allowed-transitions", "fundo-cedente", fundoId, associationId],
    queryFn: () => getFundoCedenteAllowedTransitions(fundoId!, associationId!),
    enabled: !!fundoId && !!associationId,
    staleTime: 5_000,
  });
}

// ---------------------------------------------------------------------------
// useFundoTipoAtivoAllowedTransitions
// ---------------------------------------------------------------------------

export function useFundoTipoAtivoAllowedTransitions(
  fundoId: string | undefined,
  associationId: string | undefined
) {
  return useQuery<RelationshipStatus[]>({
    queryKey: ["allowed-transitions", "fundo-tipo-ativo", fundoId, associationId],
    queryFn: () => getFundoTipoAtivoAllowedTransitions(fundoId!, associationId!),
    enabled: !!fundoId && !!associationId,
    staleTime: 5_000,
  });
}

// ---------------------------------------------------------------------------
// useCedenteTipoAtivoAllowedTransitions
// ---------------------------------------------------------------------------

export function useCedenteTipoAtivoAllowedTransitions(
  cedenteId: string | undefined,
  associationId: string | undefined
) {
  return useQuery<RelationshipStatus[]>({
    queryKey: ["allowed-transitions", "cedente-tipo-ativo", cedenteId, associationId],
    queryFn: () => getCedenteTipoAtivoAllowedTransitions(cedenteId!, associationId!),
    enabled: !!cedenteId && !!associationId,
    staleTime: 5_000,
  });
}
