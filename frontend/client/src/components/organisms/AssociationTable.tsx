// ---------------------------------------------------------------------------
// AssociationTable: shared table for N-N association aggregates (T-7)
// Used by FundoCedente, CedenteTipoAtivo, FundoTipoAtivo
// ---------------------------------------------------------------------------

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import {
  RELATIONSHIP_STATUS_LABELS,
  type RelationshipStatus,
} from "@/lib/fundos-schemas";

export interface AssociationRow {
  id: string;
  targetLabel: string;
  limitePercentual: number | null;
  limiteValor: number | null;
  dataInicio: string;
  dataFim: string | null;
  status: RelationshipStatus;
}

interface AssociationTableProps {
  rows: AssociationRow[];
  isLoading: boolean;
  canWrite: boolean;
  onEditLimits: (row: AssociationRow) => void;
  renderStatusControl: (row: AssociationRow) => React.ReactNode;
}

/**
 * Shared table component for all 3 N-N association types.
 * Renders status badge + status transition control + limits edit.
 */
export function AssociationTable({
  rows,
  isLoading,
  canWrite,
  onEditLimits: _onEditLimits,
  renderStatusControl,
}: AssociationTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2" aria-busy="true" aria-label="Carregando associações">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <div className="py-8 text-center text-muted-foreground" role="status">
        Nenhuma associação encontrada.
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Entidade</TableHead>
          <TableHead>Limite %</TableHead>
          <TableHead>Limite R$</TableHead>
          <TableHead>Início</TableHead>
          <TableHead>Fim</TableHead>
          <TableHead>Status</TableHead>
          {canWrite && <TableHead className="w-24">Ações</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {rows.map((row) => (
          <TableRow key={row.id}>
            <TableCell>{row.targetLabel}</TableCell>
            <TableCell>
              {row.limitePercentual != null ? `${row.limitePercentual}%` : "—"}
            </TableCell>
            <TableCell>
              {row.limiteValor != null
                ? row.limiteValor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
                : "—"}
            </TableCell>
            <TableCell>
              {new Date(row.dataInicio).toLocaleDateString("pt-BR")}
            </TableCell>
            <TableCell>
              {row.dataFim
                ? new Date(row.dataFim).toLocaleDateString("pt-BR")
                : "Vigência infinita"}
            </TableCell>
            <TableCell>
              <span
                className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                  row.status === "ATIVO"
                    ? "bg-green-100 text-green-800"
                    : row.status === "INATIVO"
                    ? "bg-gray-100 text-gray-600"
                    : "bg-slate-100 text-slate-500"
                }`}
              >
                {RELATIONSHIP_STATUS_LABELS[row.status]}
              </span>
            </TableCell>
            {canWrite && (
              <TableCell>
                <div className="flex items-center gap-1">
                  {renderStatusControl(row)}
                </div>
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
