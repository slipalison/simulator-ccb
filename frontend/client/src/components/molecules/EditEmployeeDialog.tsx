import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  editEmployeeSchema,
  type EditEmployeeData,
} from "@/lib/validation-schemas";
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
import { Loader2 } from "lucide-react";
import type { EmployeeDto } from "@/lib/types";

interface EditEmployeeDialogProps {
  open: boolean;
  employee: EmployeeDto;
  companyId: string;
  onSuccess: () => void;
  onOpenChange: (open: boolean) => void;
  onSave: (employeeId: string, data: EditEmployeeData) => Promise<void>;
}

export function EditEmployeeDialog({
  open,
  employee,
  companyId: _companyId,
  onSuccess,
  onOpenChange,
  onSave,
}: EditEmployeeDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<EditEmployeeData>({
    resolver: zodResolver(editEmployeeSchema),
    defaultValues: { nome: "", email: "", phone: "" },
  });

  useEffect(() => {
    if (open && employee) {
      reset({ nome: employee.nome, email: employee.email, phone: employee.phone });
    }
  }, [open, employee, reset]);

  const onSubmit = async (data: EditEmployeeData) => {
    setIsSubmitting(true);
    try {
      await onSave(employee.id, data);
      onSuccess();
      onOpenChange(false);
    } catch {
      // Error handled by parent via toast
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      reset({ nome: "", email: "", phone: "" });
      onOpenChange(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="edit-employee-dialog">
        <DialogHeader>
          <DialogTitle>Editar Funcionário</DialogTitle>
          <DialogDescription>
            Atualize o nome, email e telefone do funcionário.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-nome">Nome</Label>
            <Input
              id="edit-nome"
              {...register("nome")}
              placeholder="Nome completo"
              disabled={isSubmitting}
              autoFocus
              data-testid="edit-nome-input"
            />
            {errors.nome && (
              <p className="text-sm text-destructive" data-testid="edit-nome-error">
                {errors.nome.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-email">Email</Label>
            <Input
              id="edit-email"
              type="email"
              {...register("email")}
              placeholder="email@exemplo.com"
              disabled={isSubmitting}
              data-testid="edit-email-input"
            />
            {errors.email && (
              <p className="text-sm text-destructive" data-testid="edit-email-error">
                {errors.email.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-phone">Telefone</Label>
            <Input
              id="edit-phone"
              {...register("phone")}
              placeholder="11987654321"
              disabled={isSubmitting}
              data-testid="edit-phone-input"
            />
            {errors.phone && (
              <p className="text-sm text-destructive" data-testid="edit-phone-error">
                {errors.phone.message}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
              data-testid="edit-cancel-button"
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="default"
              disabled={isSubmitting}
              data-testid="edit-save-button"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  Salvando...
                </>
              ) : (
                "Salvar alterações"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}