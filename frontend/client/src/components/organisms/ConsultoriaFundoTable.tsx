// ---------------------------------------------------------------------------
// ConsultoriaFundoTable: paginated table for ConsultoriaFundo (T-5)
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
import type { ConsultoriaFundoDto } from "@/lib/fundos-schemas";
import { SIMPLE_STATUS_LABELS } from "@/lib/fundos-schemas";

interface ConsultoriaFundoTableProps {
  items: ConsultoriaFundoDto[];
  isLoading: boolean;
  canWrite: boolean;
  onEdit: (item: ConsultoriaFundoDto) => void;
}

export function ConsultoriaFundoTable({
  items,
  isLoading,
  canWrite,
  onEdit,
}: ConsultoriaFundoTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2" aria-busy="true" aria-label="Carregando consultorias">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground" role="status">
        Nenhuma consultoria encontrada.
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Razão Social</TableHead>
          <TableHead>Nome Fantasia</TableHead>
          <TableHead>CNPJ</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Status</TableHead>
          {canWrite && <TableHead className="w-16">Ações</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((item) => (
          <TableRow key={item.id}>
            <TableCell>{item.razaoSocial}</TableCell>
            <TableCell>{item.nomeFantasia ?? "—"}</TableCell>
            <TableCell className="font-mono">{item.cnpj}</TableCell>
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
            {canWrite && (
              <TableCell>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => onEdit(item)}
                  aria-label={`Editar consultoria ${item.razaoSocial}`}
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
