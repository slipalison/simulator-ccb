import { useState, useEffect } from "react";
import { useNavigate } from "@tanstack/react-router";
import { getUserDetail, blockUser, unblockUser, deleteUser } from "@/lib/admin-api";
import { UserDetailCard } from "@/components/molecules/UserDetailCard";
import { BlockDialog } from "@/components/molecules/BlockDialog";
import { UnblockDialog } from "@/components/molecules/UnblockDialog";
import { DeleteDialog } from "@/components/molecules/DeleteDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ArrowLeft, AlertCircle } from "lucide-react";
import { toast } from "sonner";
import type { UserDetailDto } from "@/lib/admin-api";

interface AdminUserDetailPageProps {
  userId: string;
}

export function AdminUserDetailPage({ userId }: AdminUserDetailPageProps) {
  const navigate = useNavigate();
  const [user, setUser] = useState<UserDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);
  const [isNotFound, setIsNotFound] = useState(false);
  const [blockDialogOpen, setBlockDialogOpen] = useState(false);
  const [unblockDialogOpen, setUnblockDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const fetchUser = async () => {
    if (!userId) return;
    setIsLoading(true);
    setIsError(false);
    setIsNotFound(false);
    try {
      const data = await getUserDetail(userId);
      setUser(data);
    } catch (err: unknown) {
      if (err instanceof Error && "status" in err && (err as { status?: number }).status === 404) {
        setIsNotFound(true);
      } else {
        setIsError(true);
        toast.error("Falha ao carregar usuario", { description: "Tente novamente." });
      }
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchUser();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  const handleEdit = () => {
    navigate({ to: "/admin/users/$id/edit", params: { id: userId } } as any);
  };

  const handleBlock = async (reason: string) => {
    await blockUser(userId, reason);
    const refreshed = await getUserDetail(userId);
    setUser(refreshed);
    setBlockDialogOpen(false);
  };

  const handleUnblock = async (reason?: string) => {
    await unblockUser(userId, reason);
    const refreshed = await getUserDetail(userId);
    setUser(refreshed);
    setUnblockDialogOpen(false);
  };

  const handleDelete = async (confirmEmail: string) => {
    await deleteUser(userId, confirmEmail);
    toast.success("Usuario deletado com sucesso.");
    navigate({ to: "/admin/users" as never });
  };

  if (isLoading) {
    return (
      <Card data-testid="detail-loading">
        <CardContent className="py-8">
          <Skeleton className="h-8 w-48 mb-4" />
          <Skeleton className="h-4 w-32 mb-6" />
          <Skeleton className="h-4 w-full mb-2" />
          <Skeleton className="h-4 w-full mb-2" />
          <Skeleton className="h-4 w-3/4" />
        </CardContent>
      </Card>
    );
  }

  if (isNotFound) {
    return (
      <Card data-testid="detail-not-found">
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

  if (isError || !user) {
    return (
      <Card data-testid="detail-error">
        <CardContent className="py-8 text-center">
          <AlertCircle className="h-12 w-12 text-destructive mx-auto mb-4" />
          <h2 className="text-xl font-semibold mb-2">Erro ao carregar</h2>
          <p className="text-muted-foreground mb-4">
            Nao foi possivel carregar os dados do usuario.
          </p>
          <Button onClick={fetchUser} data-testid="retry-button">
            Tentar novamente
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
        <span className="truncate" data-testid="breadcrumb-name">{user.name}</span>
      </div>

      {/* Detail card */}
      <UserDetailCard
        user={user}
        onEdit={handleEdit}
        onBlock={() => setBlockDialogOpen(true)}
        onUnblock={() => setUnblockDialogOpen(true)}
        onDelete={() => setDeleteDialogOpen(true)}
      />

      {/* Block Dialog */}
      {user && (
        <BlockDialog
          userName={user.name}
          onBlock={handleBlock}
          onClose={() => setBlockDialogOpen(false)}
          open={blockDialogOpen}
        />
      )}

      {/* Unblock Dialog */}
      {user && (
        <UnblockDialog
          userName={user.name}
          onUnblock={handleUnblock}
          onClose={() => setUnblockDialogOpen(false)}
          open={unblockDialogOpen}
        />
      )}

      {/* Delete Dialog */}
      {user && (
        <DeleteDialog
          userName={user.name}
          userEmail={user.email}
          userDocument={user.document}
          onDelete={handleDelete}
          onSuccess={() => {}}
          onClose={() => setDeleteDialogOpen(false)}
          open={deleteDialogOpen}
        />
      )}
    </div>
  );
}
