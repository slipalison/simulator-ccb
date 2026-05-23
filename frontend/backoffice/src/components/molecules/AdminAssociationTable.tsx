// ---------------------------------------------------------------------------
// AdminAssociationTable — shared table for N-N association list pages (T-6)
// Columns: Empresa, entity1, entity2, Status, DataInicio, DataFim, Limites
// ---------------------------------------------------------------------------

import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

export interface AssociationRow {
  id: string;
  empresaNome: string;
  col1Label: string;
  col1Value: string;
  col2Label: string;
  col2Value: string;
  status: string;
  dataInicio: string;
  dataFim: string | null;
  limitePercentual: number | null;
  limiteValor: number | null;
}

interface AdminAssociationTableProps {
  rows: AssociationRow[];
  col1Header: string;
  col2Header: string;
}

const STATUS_BADGE_CLASSES: Record<string, string> = {
  ATIVO: "bg-green-100 text-green-800",
  INATIVO: "bg-gray-100 text-gray-600",
  HISTORICO: "bg-blue-100 text-blue-800",
};

function formatDate(iso: string | null): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleDateString("pt-BR");
  } catch {
    return iso;
  }
}

function formatCurrency(val: number | null): string {
  if (val == null) return "—";
  return val.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function formatPercent(val: number | null): string {
  if (val == null) return "—";
  return `${val.toFixed(2)}%`;
}

export function AdminAssociationTable({
  rows,
  col1Header,
  col2Header,
}: AdminAssociationTableProps) {
  if (rows.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center" data-testid="table-empty">
        {L.emptyState}
      </p>
    );
  }

  return (
    <div className="overflow-x-auto" data-testid="association-table">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colEmpresa}</th>
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{col1Header}</th>
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{col2Header}</th>
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colStatus}</th>
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colDataInicio}</th>
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colDataFim}</th>
            <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colLimitePercentual}</th>
            <th className="pb-2 font-medium text-muted-foreground">{L.colLimiteValor}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={row.id}
              className="border-b hover:bg-muted/30 transition-colors"
              data-testid={`assoc-row-${row.id}`}
            >
              <td className="py-3 pr-4 text-muted-foreground">{row.empresaNome}</td>
              <td className="py-3 pr-4 font-medium">{row.col1Value}</td>
              <td className="py-3 pr-4">{row.col2Value}</td>
              <td className="py-3 pr-4">
                <span
                  className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_BADGE_CLASSES[row.status] ?? "bg-gray-100 text-gray-600"}`}
                >
                  {row.status}
                </span>
              </td>
              <td className="py-3 pr-4 text-muted-foreground">{formatDate(row.dataInicio)}</td>
              <td className="py-3 pr-4 text-muted-foreground">{formatDate(row.dataFim)}</td>
              <td className="py-3 pr-4 text-muted-foreground">{formatPercent(row.limitePercentual)}</td>
              <td className="py-3 text-muted-foreground">{formatCurrency(row.limiteValor)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
