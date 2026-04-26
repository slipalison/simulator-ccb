import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { EmployeeDto } from "@/lib/types";

interface EmployeeActionsDropdownProps {
  employee: EmployeeDto;
  onEdit: (employee: EmployeeDto) => void;
  onBlockUnblock: (employee: EmployeeDto) => void;
  onResetPassword: (employee: EmployeeDto) => void;
  onDelete: (employee: EmployeeDto) => void;
  onChangeAccessGroup: (employee: EmployeeDto) => void;
}

export function EmployeeActionsDropdown({
  employee,
  onEdit,
  onBlockUnblock,
  onResetPassword,
  onDelete,
  onChangeAccessGroup,
}: EmployeeActionsDropdownProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          aria-label="Abrir menu de ações"
          aria-haspopup="true"
          data-testid={`actions-dropdown-trigger-${employee.id}`}
        >
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" data-testid={`actions-dropdown-content-${employee.id}`}>
        <DropdownMenuItem
          onClick={() => onEdit(employee)}
          data-testid={`action-edit-${employee.id}`}
        >
          Editar
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={() => onBlockUnblock(employee)}
          data-testid={`action-block-unblock-${employee.id}`}
        >
          {employee.keycloakEnabled ? "Bloquear" : "Desbloquear"}
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={() => onResetPassword(employee)}
          data-testid={`action-reset-password-${employee.id}`}
        >
          Resetar senha
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={() => onChangeAccessGroup(employee)}
          data-testid={`action-change-group-${employee.id}`}
        >
          Alterar grupo de acesso
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={() => onDelete(employee)}
          className="text-destructive focus:text-destructive"
          data-testid={`action-delete-${employee.id}`}
        >
          Excluir (LGPD)
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}