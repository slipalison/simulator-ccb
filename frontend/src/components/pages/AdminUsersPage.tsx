import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

/**
 * AdminUsersPage: placeholder for Phase 18.
 * Shows a basic message until user management features are implemented.
 */
export function AdminUsersPage() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Gerenciamento de Usuarios</CardTitle>
        <CardDescription>
          Funcionalidade sera implementada na Phase 18.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-muted-foreground">
          Aqui sera exibida a lista de clientes cadastrados.
        </p>
      </CardContent>
    </Card>
  );
}
