import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { createAdmin } from "@/lib/admin-api";
import { Copy, UserPlus, Eye, EyeOff } from "lucide-react";

const createAdminSchema = z.object({
  fullName: z.string().min(2, "Nome deve ter pelo menos 2 caracteres").max(200),
  email: z.string().email("Email invalido"),
});

type CreateAdminForm = z.infer<typeof createAdminSchema>;

interface CreateAdminResult {
  adminId: string;
  temporaryPassword: string;
  email: string;
}

export function CreateAdminPage() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdResult, setCreatedResult] = useState<CreateAdminResult | null>(null);
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateAdminForm>({
    resolver: zodResolver(createAdminSchema),
    defaultValues: {
      fullName: "",
      email: "",
    },
  });

  const onSubmit = async (data: CreateAdminForm) => {
    setIsSubmitting(true);
    try {
      const result = await createAdmin(data.fullName, data.email);
      setCreatedResult({ ...result, email: data.email });
      toast.success("Admin criado com sucesso!", {
        description: "A senha temporaria foi gerada. Comunique ao novo admin.",
      });
      reset();
    } catch (err: any) {
      if (err?.status === 409) {
        toast.error("Email ja cadastrado", {
          description: "Ja existe um admin com este email.",
        });
      } else {
        toast.error("Falha ao criar admin", {
          description: "Tente novamente.",
        });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCopyPassword = () => {
    if (createdResult) {
      navigator.clipboard.writeText(createdResult.temporaryPassword);
      toast.success("Senha copiada!", { description: "Cole no canal de comunicacao com o novo admin." });
    }
  };

  const handleCreateAnother = () => {
    setCreatedResult(null);
    reset();
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <UserPlus className="h-5 w-5" />
            Criar Novo Admin
          </CardTitle>
          <CardDescription>
            Crie um novo administrador. Uma senha temporaria sera gerada e exibida para comunicacao.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="fullName">Nome Completo</Label>
              <Input
                id="fullName"
                placeholder="Ex: Joao Silva"
                {...register("fullName")}
                disabled={isSubmitting}
              />
              {errors.fullName && (
                <p className="text-sm text-red-500">{errors.fullName.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="Ex: joao@empresa.com"
                {...register("email")}
                disabled={isSubmitting}
              />
              {errors.email && (
                <p className="text-sm text-red-500">{errors.email.message}</p>
              )}
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Criando..." : "Criar Admin"}
            </Button>
          </form>
        </CardContent>
      </Card>

      {createdResult && (
        <Card className="border-green-200 bg-green-50 dark:bg-green-950 dark:border-green-800">
          <CardHeader>
            <CardTitle className="text-green-700 dark:text-green-300">Admin Criado com Sucesso!</CardTitle>
            <CardDescription className="text-green-600 dark:text-green-400">
              A senha temporaria abaixo deve ser comunicada ao novo admin. Ele sera forcado a troca-la no primeiro login.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label>Email do novo admin</Label>
              <p className="font-mono text-sm bg-white dark:bg-gray-900 p-2 rounded border">
                {createdResult.email}
              </p>
            </div>

            <div className="space-y-2">
              <Label>Senha Temporaria</Label>
              <div className="flex gap-2">
                <div className="flex-1 relative">
                  <Input
                    type={showPassword ? "text" : "password"}
                    value={createdResult.temporaryPassword}
                    readOnly
                    className="font-mono pr-10"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700"
                    aria-label={showPassword ? "Ocultar senha" : "Mostrar senha"}
                  >
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                <Button variant="outline" size="icon" onClick={handleCopyPassword} title="Copiar senha">
                  <Copy className="h-4 w-4" />
                </Button>
              </div>
            </div>

            <Button variant="secondary" onClick={handleCreateAnother} className="w-full">
              Criar Outro Admin
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
