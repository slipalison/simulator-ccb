import { useState, useEffect } from "react";
import { useNavigate } from "@tanstack/react-router";
import { getUserDetail, updateUser, type UserDetailDto } from "@/lib/admin-api";
import { EditUserForm } from "@/components/molecules/EditUserForm";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ArrowLeft, AlertCircle } from "lucide-react";
import { toast } from "sonner";

interface AdminUserEditPageProps {
  userId: string;
}

export function AdminUserEditPage({ userId }: AdminUserEditPageProps) {
  const navigate = useNavigate();
  const [user, setUser] = useState<UserDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isNotFound, setIsNotFound] = useState(false);

  useEffect(() => {
    const fetchUser = async () => {
      setIsLoading(true);
      setIsNotFound(false);
      try {
        const data = await getUserDetail(userId);
        setUser(data);
      } catch (err: unknown) {
        if (err instanceof Error && "status" in err && (err as { status?: number }).status === 404) {
          setIsNotFound(true);
        } else {
          toast.error("Falha ao carregar usuario", { description: "Tente novamente." });
        }
      } finally {
        setIsLoading(false);
      }
    };
    fetchUser();
  }, [userId]);

  const handleUpdate = async (data: { name?: string; email?: string; phone?: string; address?: string }) => {
    await updateUser(userId, data);
    // Refresh user data
    const refreshed = await getUserDetail(userId);
    setUser(refreshed);
  };

  const handleCancel = () => {
    navigate({ to: "/admin/users/$id", params: { id: userId } } as any);
  };

  const handleSuccess = () => {
    navigate({ to: "/admin/users/$id", params: { id: userId } } as any);
  };

  if (isLoading) {
    return (
      <Card data-testid="edit-loading">
        <CardContent className="py-8">
          <Skeleton className="h-8 w-48 mb-4" />
          <Skeleton className="h-4 w-32 mb-6" />
          <Skeleton className="h-10 w-full mb-2" />
          <Skeleton className="h-10 w-full mb-2" />
          <Skeleton className="h-10 w-3/4" />
        </CardContent>
      </Card>
    );
  }

  if (isNotFound || !user) {
    return (
      <Card data-testid="edit-not-found">
        <CardContent className="py-8 text-center">
          <AlertCircle className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h2 className="text-xl font-semibold mb-2">Usuario nao encontrado</h2>
          <p className="text-muted-foreground mb-4">
            O usuario solicitado nao existe ou foi removido.
          </p>
          <Button
            onClick={() => navigate({ to: "/admin/users" as never })}
            data-testid="back-to-list-button"
          >
            <ArrowLeft className="h-4 w-4 mr-1" />
            Voltar para lista
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground" data-testid="breadcrumb">
        <Button
          variant="link"
          onClick={() => navigate({ to: "/admin/users" as never })}
          className="p-0 h-auto"
          data-testid="breadcrumb-back"
        >
          <ArrowLeft className="h-3 w-3 mr-1" />
          Usuarios
        </Button>
        <span>/</span>
        <Button
          variant="link"
          onClick={() => navigate({ to: "/admin/users/$id", params: { id: userId } } as any)}
          className="p-0 h-auto"
          data-testid="breadcrumb-name"
        >
          {user.name}
        </Button>
        <span>/</span>
        <span className="truncate">Editar</span>
      </div>

      <EditUserForm
        user={user}
        onUpdate={handleUpdate}
        onCancel={handleCancel}
        onSuccess={handleSuccess}
      />
    </div>
  );
}
