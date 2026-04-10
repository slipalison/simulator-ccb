import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader2 } from "lucide-react";

// ---------------------------------------------------------------------------
// Schema
// ---------------------------------------------------------------------------

const adminLoginSchema = z.object({
  email: z.string().min(1, "Email e obrigatorio").email("Email invalido"),
  password: z.string().min(1, "Senha e obrigatoria"),
});

export type AdminLoginData = z.infer<typeof adminLoginSchema>;

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface AdminLoginFormProps {
  onSubmit: (data: AdminLoginData) => void | Promise<void>;
  serverError?: string | null;
  isLoading?: boolean;
}

// ---------------------------------------------------------------------------
// Molecule: admin login form with shadcn/ui + RHF + Zod validation
// ---------------------------------------------------------------------------

export function AdminLoginForm({ onSubmit, serverError, isLoading }: AdminLoginFormProps) {
  const form = useForm<AdminLoginData>({
    resolver: zodResolver(adminLoginSchema),
    defaultValues: { email: "", password: "" },
  });

  const { isSubmitting } = form.formState;
  const disabled = isSubmitting || isLoading;

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-4 w-full"
        noValidate
      >
        {serverError && (
          <Alert variant="destructive" role="alert">
            <AlertDescription>{serverError}</AlertDescription>
          </Alert>
        )}

        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input
                  type="email"
                  placeholder="admin@onboarding.local"
                  autoComplete="email"
                  disabled={disabled}
                  data-testid="admin-email"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Senha</FormLabel>
              <FormControl>
                <Input
                  type="password"
                  autoComplete="current-password"
                  disabled={disabled}
                  data-testid="admin-password"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button
          type="submit"
          className="w-full"
          disabled={disabled}
          data-testid="admin-login-button"
        >
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {isLoading ? "Entrando..." : "Entrar"}
        </Button>
      </form>
    </Form>
  );
}
