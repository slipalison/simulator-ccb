import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { TriangleAlert } from "lucide-react";

/**
 * AdminAccessDeniedPage: shown when a non-admin or unauthorized user
 * tries to access admin-only areas.
 */
export function AdminAccessDeniedPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <div className="flex items-center justify-center mb-2">
            <TriangleAlert className="h-12 w-12 text-destructive" />
          </div>
          <CardTitle className="text-2xl text-center">Acesso Negado</CardTitle>
          <CardDescription className="text-center">
            Voce nao tem permissao para acessar esta area.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex justify-center">
          <a href="/">
            <Button variant="outline">Voltar para Home</Button>
          </a>
        </CardContent>
      </Card>
    </div>
  );
}
