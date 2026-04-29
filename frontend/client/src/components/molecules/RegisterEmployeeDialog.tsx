import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  registerEmployeeSchema,
  type RegisterEmployeeData,
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
import { getAccessGroups, registerEmployee, type AccessGroupDto } from "@/lib/api";

interface RegisterEmployeeDialogProps {
  open: boolean;
  companyId: string;
  onRegistered: (result: { employeeId: string; temporaryPassword: string }) => void;
  onClose: () => void;
}

export function RegisterEmployeeDialog({
  open,
  companyId,
  onRegistered,
  onClose,
}: RegisterEmployeeDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [_accessGroups, setAccessGroups] = useState<AccessGroupDto[]>([]);
  const [selectedAccessGroupId, setSelectedAccessGroupId] = useState<string>("");
  const [isLoadingGroups, setIsLoadingGroups] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<RegisterEmployeeData>({
    resolver: zodResolver(registerEmployeeSchema),
    defaultValues: { nome: "", cpf: "", email: "", phone: "" },
  });

  useEffect(() => {
    if (!open || !companyId) return;
    setIsLoadingGroups(true);
    getAccessGroups(companyId)
      .then((groups) => {
        setAccessGroups(groups);
        const viewer = groups.find((g) => g.name === "viewer");
        if (viewer) setSelectedAccessGroupId(viewer.id);
      })
      .catch(() => setAccessGroups([]))
      .finally(() => setIsLoadingGroups(false));
  }, [open, companyId]);

  const onSubmit = async (data: RegisterEmployeeData) => {
    if (!companyId) return;
    setIsSubmitting(true);
    setApiError(null);
    try {
      const result = await registerEmployee(companyId, {
        nome: data.nome,
        cpf: data.cpf,
        email: data.email,
        phone: data.phone,
        accessGroupId: selectedAccessGroupId || undefined,
      });
      onRegistered(result);
      reset({ nome: "", cpf: "", email: "", phone: "" });
    } catch (err: unknown) {
      if (err && typeof err === "object" && "message" in err) {
        setApiError((err as { message: string }).message);
      } else {
        setApiError("Erro ao cadastrar funcionário. Tente novamente.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen && !isSubmitting) {
      reset({ nome: "", cpf: "", email: "", phone: "" });
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent data-testid="register-employee-dialog" className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Novo Funcionário</DialogTitle>
          <DialogDescription>
            Cadastre um funcionário vinculado à sua empresa. Uma senha temporária será gerada automaticamente.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {apiError && (
            <p className="text-sm text-destructive" data-testid="reg-api-error">{apiError}</p>
          )}
          <div className="space-y-2">
            <Label htmlFor="reg-nome">Nome</Label>
            <Input
              id="reg-nome"
              {...register("nome")}
              placeholder="Nome completo"
              disabled={isSubmitting}
              autoFocus
              data-testid="reg-nome-input"
            />
            {errors.nome && (
              <p className="text-sm text-destructive" data-testid="reg-nome-error">{errors.nome.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="reg-cpf">CPF</Label>
            <Input
              id="reg-cpf"
              {...register("cpf")}
              placeholder="Apenas números (11 dígitos)"
              disabled={isSubmitting}
              data-testid="reg-cpf-input"
            />
            {errors.cpf && (
              <p className="text-sm text-destructive" data-testid="reg-cpf-error">{errors.cpf.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="reg-email">Email</Label>
            <Input
              id="reg-email"
              type="email"
              {...register("email")}
              placeholder="email@exemplo.com"
              disabled={isSubmitting}
              data-testid="reg-email-input"
            />
            {errors.email && (
              <p className="text-sm text-destructive" data-testid="reg-email-error">{errors.email.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="reg-phone">Telefone</Label>
            <Input
              id="reg-phone"
              {...register("phone")}
              placeholder="11987654321"
              disabled={isSubmitting}
              data-testid="reg-phone-input"
            />
            {errors.phone && (
              <p className="text-sm text-destructive" data-testid="reg-phone-error">{errors.phone.message}</p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
              data-testid="reg-cancel-button"
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="default"
              disabled={isSubmitting || isLoadingGroups}
              data-testid="reg-submit-button"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  Cadastrando...
                </>
              ) : (
                "Cadastrar funcionário"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}