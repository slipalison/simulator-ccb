// ---------------------------------------------------------------------------
// CedenteTable: paginated table for Cedente (T-4)
// ---------------------------------------------------------------------------

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Pencil } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import type { CedenteDto } from "@/lib/fundos-schemas";
import { SIMPLE_STATUS_LABELS } from "@/lib/fundos-schemas";

interface CedenteTableProps {
  items: CedenteDto[];
  isLoading: boolean;
  canWrite: boolean;
  onEdit: (item: CedenteDto) => void;
  onDetail: (item: CedenteDto) => void;
}

export function CedenteTable({
  items,
  isLoading,
  canWrite,
  onEdit,
  onDetail,
}: CedenteTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2" aria-busy="true" aria-label="Carregando cedentes">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground" role="status">
        Nenhum cedente encontrado.
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Nome</TableHead>
          <TableHead>Tipo</TableHead>
          <TableHead>CPF/CNPJ</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="w-24">Ações</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((item) => (
          <TableRow key={item.id}>
            <TableCell>
              <button
                type="button"
                onClick={() => onDetail(item)}
                className="font-medium text-primary hover:underline"
                aria-label={`Ver detalhes de ${item.nome}`}
              >
                {item.nome}
              </button>
            </TableCell>
            <TableCell>
              <span className="inline-flex rounded-full px-2 py-0.5 text-xs font-medium bg-blue-100 text-blue-800">
                {item.cedenteTipo}
              </span>
            </TableCell>
            <TableCell className="font-mono">{item.documento}</TableCell>
            <TableCell>{item.email ?? "—"}</TableCell>
            <TableCell>
              <span
                className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                  item.status === "ATIVO"
                    ? "bg-green-100 text-green-800"
                    : "bg-gray-100 text-gray-600"
                }`}
              >
                {SIMPLE_STATUS_LABELS[item.status] ?? item.status}
              </span>
            </TableCell>
            <TableCell>
              {canWrite && (
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => onEdit(item)}
                  aria-label={`Editar cedente ${item.nome}`}
                >
                  <Pencil className="h-4 w-4" aria-hidden="true" />
                </Button>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
