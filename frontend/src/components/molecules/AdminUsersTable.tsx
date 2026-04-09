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
import type { UserSummaryDto } from "@/lib/admin-api";

interface AdminUsersTableProps {
  users: UserSummaryDto[];
  onViewDetails: (id: string) => void;
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
          return (
            <TableRow key={user.id} data-testid={`user-row-${user.id}`}>
              <TableCell className="font-medium">{user.name}</TableCell>
              <TableCell>{user.document || "-"}</TableCell>
              <TableCell>{user.email}</TableCell>
              <TableCell>
                <Badge variant={status.variant} data-testid={`status-badge-${user.id}`}>
                  {status.text}
                </Badge>
              </TableCell>
              <TableCell>
                <Button
                  variant="link"
                  className="p-0 h-auto"
                  onClick={() => onViewDetails(user.id)}
                  data-testid={`view-details-${user.id}`}
                >
                  Ver
                </Button>
              </TableCell>
            </TableRow>
          );
        })}
      </TableBody>
    </Table>
  );
}
