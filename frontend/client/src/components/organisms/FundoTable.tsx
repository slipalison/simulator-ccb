// ---------------------------------------------------------------------------
// FundoTable: paginated table for Fundo (T-6)
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
import { Pencil, Eye } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import type { FundoDto } from "@/lib/fundos-schemas";
import { FundoStatusBadge } from "@/components/organisms/FundoStatusBadge";
import { TIPO_FUNDO_LABELS } from "@/lib/fundos-schemas";

interface FundoTableProps {
  items: FundoDto[];
  isLoading: boolean;
  canWrite: boolean;
  onEdit: (item: FundoDto) => void;
  onDetail: (item: FundoDto) => void;
}

export function FundoTable({
  items,
  isLoading,
  canWrite,
  onEdit,
  onDetail,
}: FundoTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2" aria-busy="true" aria-label="Carregando fundos">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground" role="status">
        Nenhum fundo encontrado.
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Nome</TableHead>
          <TableHead>CNPJ</TableHead>
          <TableHead>Tipo</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Constituição</TableHead>
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
                aria-label={`Ver detalhes do fundo ${item.nome}`}
              >
                {item.nome}
              </button>
            </TableCell>
            <TableCell className="font-mono">{item.cnpj}</TableCell>
            <TableCell>{TIPO_FUNDO_LABELS[item.tipoFundo] ?? item.tipoFundo}</TableCell>
            <TableCell>
              <FundoStatusBadge status={item.status} />
            </TableCell>
            <TableCell>
              {item.dataConstituicao
                ? new Date(item.dataConstituicao).toLocaleDateString("pt-BR")
                : "—"}
            </TableCell>
            <TableCell>
              <div className="flex gap-1">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => onDetail(item)}
                  aria-label={`Ver detalhes do fundo ${item.nome}`}
                >
                  <Eye className="h-4 w-4" aria-hidden="true" />
                </Button>
                {canWrite && (
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onEdit(item)}
                    aria-label={`Editar fundo ${item.nome}`}
                  >
                    <Pencil className="h-4 w-4" aria-hidden="true" />
                  </Button>
                )}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
