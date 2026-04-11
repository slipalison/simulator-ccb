import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Pencil, Ban, CheckCircle, Trash2 } from "lucide-react";
import type { UserSummaryDto } from "@/lib/admin-api";

interface AdminUsersTableProps {
  users: UserSummaryDto[];
  onViewDetails: (id: string) => void;
  onEdit?: (id: string) => void;
  onBlock?: (id: string) => void;
  onUnblock?: (id: string) => void;
  onDelete?: (id: string) => void;
  isLoading?: boolean;
  isEmpty?: boolean;
  isError?: boolean;
  onRetry?: () => void;
}

function getStatusInfo(user: UserSummaryDto): { text: string; variant: "default" | "secondary" | "destructive" } {
  if (user.deletedAt) {
    return { text: "Deletado", variant: "destructive" };
  }
  if (user.enabled) {
    return { text: "Ativo", variant: "default" };
  }
  return { text: "Bloqueado", variant: "secondary" };
}

export function AdminUsersTable({
  users,
  onViewDetails,
  onEdit,
  onBlock,
  onUnblock,
  onDelete,
  isLoading = false,
  isEmpty = false,
  isError = false,
  onRetry,
}: AdminUsersTableProps) {
  if (isLoading) {
    return (
      <Table data-testid="users-table">
        <TableHeader>
          <TableRow>
            <TableHead>Nome</TableHead>
            <TableHead>Documento</TableHead>
            <TableHead>Email</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Acoes</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {Array.from({ length: 5 }).map((_, i) => (
            <TableRow key={i} data-testid={`skeleton-row-${i}`}>
              <TableCell><Skeleton className="h-4 w-[150px]" /></TableCell>
              <TableCell><Skeleton className="h-4 w-[100px]" /></TableCell>
              <TableCell><Skeleton className="h-4 w-[180px]" /></TableCell>
              <TableCell><Skeleton className="h-5 w-[60px] rounded-full" /></TableCell>
              <TableCell><Skeleton className="h-4 w-[50px]" /></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-8 text-center" data-testid="error-state">
        <p className="text-destructive font-medium mb-2">Erro ao carregar usuarios</p>
        <p className="text-sm text-muted-foreground mb-4">
          Nao foi possivel carregar a lista de usuarios. Tente novamente.
        </p>
        {onRetry && (
          <Button variant="outline" onClick={onRetry} data-testid="retry-button">
            Tentar novamente
          </Button>
        )}
      </div>
    );
  }

  if (isEmpty) {
    return (
      <div className="flex flex-col items-center justify-center py-8 text-center" data-testid="empty-state">
        <p className="text-muted-foreground">Nenhum usuario encontrado</p>
      </div>
    );
  }

  return (
    <Table data-testid="users-table">
      <TableHeader>
        <TableRow>
          <TableHead>Nome</TableHead>
          <TableHead>Documento</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Acoes</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => {
          const status = getStatusInfo(user);
          const isDeleted = !!user.deletedAt;
          return (
            <TableRow
              key={user.id}
              data-testid={`user-row-${user.id}`}
              className={isDeleted ? "opacity-60" : ""}
            >
              <TableCell className="font-medium">{user.name}</TableCell>
              <TableCell>{user.document || "-"}</TableCell>
              <TableCell>{user.email}</TableCell>
              <TableCell>
                <Badge variant={status.variant} data-testid={`status-badge-${user.id}`}>
                  {status.text}
                </Badge>
              </TableCell>
              <TableCell>
                <div className="flex gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => onViewDetails(user.id)}
                    data-testid={`view-details-${user.id}`}
                    title="Ver detalhes"
                  >
                    <span className="sr-only">Ver</span>
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/></svg>
                  </Button>
                  {onEdit && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => onEdit!(user.id)}
                      data-testid={`edit-action-${user.id}`}
                      title="Editar"
                      disabled={isDeleted}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                  )}
                  {!isDeleted && onBlock && user.enabled && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={() => onBlock!(user.id)}
                      data-testid={`block-action-${user.id}`}
                      title="Block user"
                    >
                      <Ban className="h-4 w-4" />
                    </Button>
                  )}
                  {!isDeleted && onUnblock && !user.enabled && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-green-600"
                      onClick={() => onUnblock!(user.id)}
                      data-testid={`unblock-action-${user.id}`}
                      title="Unblock user"
                    >
                      <CheckCircle className="h-4 w-4" />
                    </Button>
                  )}
                  {onDelete && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={() => onDelete!(user.id)}
                      data-testid={`delete-action-${user.id}`}
                      title="Delete user (LGPD)"
                      disabled={isDeleted}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </TableCell>
            </TableRow>
          );
        })}
      </TableBody>
    </Table>
  );
}
