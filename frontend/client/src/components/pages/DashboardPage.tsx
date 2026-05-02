import { useEffect, useState } from "react";
import { useAuth, getDefaultRouteForGroup } from "@/lib/auth-context";
import { Navigate } from "@tanstack/react-router";
import { getEmployees } from "@/lib/api";
import {
  TotalEmployeesCard,
  ActiveEmployeesCard,
  BlockedEmployeesCard,
  RecentLoginsCard,
  RecentActionsCard,
  LastLoginCard,
} from "@/components/molecules/DashboardCards";

/**
 * DashboardPage: 6-card mock dashboard per D-15.
 * Accessible by admin-empresa and dashboard groups.
 * Total Funcionários card can optionally show real API count (D-18).
 * Unauthorized users are redirected to their group's default route (D-22).
 */
export function DashboardPage() {
  const { auth } = useAuth();
  const [totalEmployees, setTotalEmployees] = useState<number | null>(null);

  // Fetch real employee count for Total Funcionários card (D-18 exception)
  useEffect(() => {
    if (auth.isAuthenticated && auth.companyId && (auth.accessGroup === "admin-empresa" || auth.accessGroup === "dashboard")) {
      getEmployees(auth.companyId, { page: 1, pageSize: 1 })
        .then((result) => {
          setTotalEmployees(result.totalCount);
        })
        .catch(() => {
          // Fallback to mock data silently
        });
    }
  }, [auth.isAuthenticated, auth.companyId, auth.accessGroup]);

  // Auth guard: redirect unauthenticated users
  if (!auth.isAuthenticated) {
    return <Navigate to="/auth/login" />;
  }

  // Permission guard: only admin-empresa and dashboard can see dashboard
  // Redirect unauthorized users to their group's default route (D-22)
  if (auth.accessGroup !== "admin-empresa" && auth.accessGroup !== "dashboard") {
    return <Navigate to={getDefaultRouteForGroup(auth.accessGroup) as any} />;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Dashboard</h1>
        {auth.userName && (
          <p className="text-sm text-muted-foreground">
            Bem-vindo(a), {auth.userName}
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-2">
        <TotalEmployeesCard
          count={totalEmployees ?? undefined}
        />
        <ActiveEmployeesCard />
        <BlockedEmployeesCard />
        <RecentLoginsCard />
        <RecentActionsCard />
        <LastLoginCard />
      </div>
    </div>
  );
}