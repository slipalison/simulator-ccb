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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { EmployeeApiError } from "@/lib/api";
import type { EmployeeDto } from "@/lib/types";

interface DeleteEmployeeDialogProps {
  open: boolean;
  employee: EmployeeDto;
  companyId: string;
  onDelete: (employeeId: string) => Promise<void>;
  onSuccess: () => void;
  onClose: () => void;
}

export function DeleteEmployeeDialog({
  open,
  employee,
  companyId: _companyId,
  onDelete,
  onSuccess,
  onClose,
}: DeleteEmployeeDialogProps) {
  const [emailInput, setEmailInput] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const emailMatches = emailInput.trim().toLowerCase() === employee.email.toLowerCase();

  const handleDelete = async () => {
    if (!emailMatches) return;
    setIsSubmitting(true);
    try {
      await onDelete(employee.id);
      toast.success("Funcionário excluído (LGPD).");
      setEmailInput("");
      onSuccess();
      onClose();
    } catch (err) {
      if (err instanceof EmployeeApiError && err.status === 404) {
        toast.error("Funcionário já foi excluído.");
      } else {
        toast.error("Falha ao excluir funcionário", { description: "Tente novamente." });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setEmailInput("");
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="delete-employee-dialog">
        <DialogHeader>
          <DialogTitle>Excluir Funcionário (LGPD)</DialogTitle>
          <DialogDescription>
            Esta ação é irreversível. Os dados do funcionário serão anonimizados.
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            Esta ação é PERMANENTE e não pode ser desfeita. Todos os dados do funcionário
            serão anonimizados e removidos do Keycloak, em conformidade com a LGPD.
          </AlertDescription>
        </Alert>

        <div className="space-y-3 py-2">
          <div className="space-y-1">
            <Label className="text-sm font-medium">Funcionário a excluir</Label>
            <div className="text-sm text-muted-foreground">
              <p><span className="font-medium">Nome:</span> {employee.nome}</p>
              <p><span className="font-medium">Email:</span> {employee.email}</p>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="delete-email-confirm">
              Digite o email do funcionário para confirmar:
            </Label>
            <Input
              id="delete-email-confirm"
              type="email"
              value={emailInput}
              onChange={(e) => setEmailInput(e.target.value)}
              placeholder={employee.email}
              disabled={isSubmitting}
              data-testid="delete-email-confirm-input"
              autoComplete="off"
            />
            {emailInput.length > 0 && !emailMatches && (
              <p className="text-sm text-destructive" data-testid="email-mismatch-error">
                Email não confere. Digite o email exato para confirmar a exclusão.
              </p>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={isSubmitting}
            data-testid="delete-cancel-button"
          >
            Cancelar
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={isSubmitting || !emailMatches}
            onClick={handleDelete}
            data-testid="confirm-delete-button"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                Excluindo...
              </>
            ) : (
              "Excluir Funcionário"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}