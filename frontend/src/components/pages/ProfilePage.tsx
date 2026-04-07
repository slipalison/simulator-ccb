import { useEffect } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/lib/auth-context";
import { PageLayout } from "@/components/templates/PageLayout";

/**
 * ProfilePage: placeholder for Phase 10.
 * Redirects unauthenticated users to /login.
 * Shows placeholder text with logout button for authenticated users.
 */
export function ProfilePage() {
  const { auth, logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!auth.isAuthenticated) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/login" as any, replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  if (!auth.isAuthenticated) {
    return null; // Will redirect via useEffect
  }

  const handleLogout = () => {
    logout();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    navigate({ to: "/login" as any, replace: true });
  };

  return (
    <PageLayout>
      <div className="mx-auto max-w-md text-center">
        <h1 className="mb-4 text-3xl font-bold text-foreground">Profile</h1>
        <p className="mb-6 text-muted-foreground">
          Profile page — will be implemented in Phase 10.
        </p>
        <button
          onClick={handleLogout}
          className="rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700"
        >
          Logout
        </button>
      </div>
    </PageLayout>
  );
}
