import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { adminEditUserSchema, type AdminEditUserInput } from "@/lib/validation-schemas";
import type { UserDetailDto } from "@/lib/admin-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";

interface EditUserFormProps {
  user: UserDetailDto;
  onUpdate: (data: { name?: string; email?: string; phone?: string; address?: string }) => Promise<void>;
  onCancel: () => void;
  onSuccess: () => void;
}

export function EditUserForm({ user, onUpdate, onCancel, onSuccess }: EditUserFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<AdminEditUserInput>({
    resolver: zodResolver(adminEditUserSchema as any),
    defaultValues: {
      name: user.name,
      email: user.email,
      phone: user.phone,
      address: "",
    },
  });

  const onSubmit = async (data: AdminEditUserInput) => {
    try {
      // Filter out empty strings to send only meaningful values
      const payload: { name?: string; email?: string; phone?: string; address?: string } = {};
      if (data.name && data.name.trim()) payload.name = data.name.trim();
      if (data.email && data.email.trim()) payload.email = data.email.trim();
      if (data.phone && data.phone.trim()) payload.phone = data.phone.trim();
      if (data.address && data.address.trim()) payload.address = data.address.trim();

      await onUpdate(payload);
      toast.success("Usuario atualizado com sucesso.");
      onSuccess();
    } catch {
      toast.error("Falha ao atualizar usuario", {
        description: "Tente novamente.",
      });
    }
  };

  const isPF = user.type === "PF";

  return (
    <Card data-testid="edit-user-form">
      <CardHeader>
        <CardTitle>Editar Usuario</CardTitle>
        <p className="text-sm text-muted-foreground">
          Edite os dados do usuario. Os campos marcados sao obrigatorios.
        </p>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {/* Readonly badges */}
          <div className="flex gap-2 mb-4">
            <Badge variant="secondary" data-testid="person-type-badge">
              {isPF ? "Pessoa Fisica" : "Pessoa Juridica"}
            </Badge>
            <Badge variant="outline" data-testid="document-badge">
              {user.document || "Sem documento"}
            </Badge>
          </div>

          <Separator />

          {/* Name */}
          <div className="space-y-2">
            <Label htmlFor="name">Nome</Label>
            <Input
              id="name"
              {...register("name")}
              data-testid="name-input"
              disabled={isSubmitting}
            />
            {errors.name && (
              <p className="text-sm text-destructive" data-testid="name-error">
                {errors.name.message}
              </p>
            )}
          </div>

          {/* Email */}
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="text"
              {...register("email")}
              data-testid="email-input"
              disabled={isSubmitting}
            />
            {errors.email && (
              <p className="text-sm text-destructive" data-testid="email-error">
                {errors.email.message}
              </p>
            )}
          </div>

          {/* Phone */}
          <div className="space-y-2">
            <Label htmlFor="phone">Telefone</Label>
            <Input
              id="phone"
              {...register("phone")}
              data-testid="phone-input"
              placeholder="(XX) XXXXX-XXXX"
              disabled={isSubmitting}
            />
            {errors.phone && (
              <p className="text-sm text-destructive" data-testid="phone-error">
                {errors.phone.message}
              </p>
            )}
          </div>

          {/* Address */}
          <div className="space-y-2">
            <Label htmlFor="address">Endereco (opcional)</Label>
            <Input
              id="address"
              {...register("address")}
              data-testid="address-input"
              disabled={isSubmitting}
            />
            {errors.address && (
              <p className="text-sm text-destructive" data-testid="address-error">
                {errors.address.message}
              </p>
            )}
          </div>

          {/* Razao Social for PJ */}
          {user.razaoSocial && (
            <div className="space-y-2">
              <Label>Razao Social</Label>
              <p className="text-sm text-muted-foreground" data-testid="razao-social-display">
                {user.razaoSocial}
              </p>
            </div>
          )}

          <Separator />

          {/* Actions */}
          <div className="flex gap-2 justify-end">
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={isSubmitting}
              data-testid="cancel-button"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              data-testid="save-button"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  Saving...
                </>
              ) : (
                "Save Changes"
              )}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
