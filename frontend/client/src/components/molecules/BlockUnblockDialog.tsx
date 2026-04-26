import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle, Loader2 } from "lucide-react";
import type { EmployeeDto } from "@/lib/types";

interface BlockUnblockDialogProps {
  open: boolean;
  employee: EmployeeDto;
  action: "block" | "unblock";
  onConfirm: (employeeId: string, activate: boolean) => Promise<void>;
  onClose: () => void;
}

export function BlockUnblockDialog({
  open,
  employee,
  action,
  onConfirm,
  onClose,
}: BlockUnblockDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleConfirm = async () => {
    setIsSubmitting(true);
    try {
      await onConfirm(employee.id, action === "unblock");
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen && !isSubmitting) {
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="block-unblock-dialog">
        <DialogHeader>
          <DialogTitle>
            {action === "block" ? "Bloquear Funcionário" : "Desbloquear Funcionário"}
          </DialogTitle>
          <DialogDescription>
            {action === "block"
              ? `Tem certeza que deseja bloquear ${employee.nome}? O funcionário perderá acesso ao sistema.`
              : `Tem certeza que deseja desbloquear ${employee.nome}? O funcionário poderá acessar o sistema novamente.`}
          </DialogDescription>
        </DialogHeader>

        {action === "block" && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              Esta ação pode ser revertida desbloqueando o funcionário posteriormente.
            </AlertDescription>
          </Alert>
        )}

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={isSubmitting}
            data-testid="block-unblock-cancel-button"
          >
            Cancelar
          </Button>
          <Button
            type="button"
            variant={action === "block" ? "destructive" : "default"}
            onClick={handleConfirm}
            disabled={isSubmitting}
            data-testid="block-unblock-confirm-button"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                {action === "block" ? "Bloqueando..." : "Desbloqueando..."}
              </>
            ) : action === "block" ? (
              "Bloquear"
            ) : (
              "Desbloquear"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}