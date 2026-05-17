// ---------------------------------------------------------------------------
// TipoAtivoTable: paginated table for TipoAtivo (T-3)
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
import type { TipoAtivoDto } from "@/lib/fundos-schemas";
import {
  SIMPLE_STATUS_LABELS,
  TIPO_ATIVO_CATEGORIA_LABELS,
} from "@/lib/fundos-schemas";

interface TipoAtivoTableProps {
  items: TipoAtivoDto[];
  isLoading: boolean;
  canWrite: boolean;
  onEdit: (item: TipoAtivoDto) => void;
}

export function TipoAtivoTable({
  items,
  isLoading,
  canWrite,
  onEdit,
}: TipoAtivoTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2" aria-busy="true" aria-label="Carregando tipos de ativo">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground" role="status">
        Nenhum tipo de ativo encontrado.
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Código</TableHead>
          <TableHead>Descrição</TableHead>
          <TableHead>Categoria</TableHead>
          <TableHead>Subcategoria</TableHead>
          <TableHead>Ordem</TableHead>
          <TableHead>Status</TableHead>
          {canWrite && <TableHead className="w-16">Ações</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((item) => (
          <TableRow key={item.id}>
            <TableCell className="font-mono">{item.codigo}</TableCell>
            <TableCell>{item.descricao}</TableCell>
            <TableCell>{TIPO_ATIVO_CATEGORIA_LABELS[item.categoria] ?? item.categoria}</TableCell>
            <TableCell>{item.subcategoria ?? "—"}</TableCell>
            <TableCell>{item.ordemExibicao}</TableCell>
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
            {canWrite && (
              <TableCell>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => onEdit(item)}
                  aria-label={`Editar tipo de ativo ${item.codigo}`}
                >
                  <Pencil className="h-4 w-4" aria-hidden="true" />
                </Button>
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
