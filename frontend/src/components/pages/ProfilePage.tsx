import { useState, useEffect } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/lib/auth-context";
import { getProfileClient } from "@/lib/api";
import type { ClientProfileDto } from "@/lib/types";
import { ProfileCard } from "@/components/molecules/ProfileCard";
import { PageLayout } from "@/components/templates/PageLayout";
import { AppButton } from "@/components/atoms/AppButton";

/**
 * ProfilePage: exibe dados cadastrais do cliente autenticado.
 * Redireciona para /login se não autenticado.
 * Busca dados via getProfileClient() ao montar.
 */
export function ProfilePage() {
  const { auth, logout } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<ClientProfileDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Auth guard — redireciona para login se não autenticado
  useEffect(() => {
    if (!auth.isAuthenticated) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/login" as any, replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  // Busca dados do perfil ao montar
  useEffect(() => {
    async function fetchProfile() {
      if (!auth.isAuthenticated) return;

      try {
        setIsLoading(true);
        setError(null);
        const data = await getProfileClient();
        setProfile(data);
      } catch (err) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError("Falha ao carregar dados do perfil");
        }
      } finally {
        setIsLoading(false);
      }
    }

    fetchProfile();
  }, [auth.isAuthenticated]);

  function handleLogout() {
    logout();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    navigate({ to: "/login" as any, replace: true });
  }

  return (
    <PageLayout>
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex justify-between items-center">
          <h1 className="text-2xl font-bold">Meu Perfil</h1>
          <AppButton variant="outline" onClick={handleLogout}>
            Sair
          </AppButton>
        </div>

        {isLoading && (
          <div className="text-center py-8" data-testid="profile-loading">
            <p>Carregando perfil...</p>
          </div>
        )}

        {error && (
          <div
            className="bg-red-50 border border-red-200 rounded-lg p-4"
            data-testid="profile-error"
          >
            <p className="text-red-800">Erro ao carregar perfil: {error}</p>
          </div>
        )}

        {profile && !isLoading && <ProfileCard profile={profile} />}
      </div>
    </PageLayout>
  );
}
